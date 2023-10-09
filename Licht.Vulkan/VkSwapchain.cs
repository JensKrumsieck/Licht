using Licht.Vulkan.Extensions;
using Licht.Vulkan.Memory;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Licht.Vulkan;

public sealed unsafe class VkSwapchain : IDisposable
{
    public const int MaxImagesInFlight = 2;

    public int ImageCount => _swapchainImages.Length;
    public Extent2D Extent => _extent;
    public Format DepthFormat => _depthFormat;
    public Format ImageFormat => _imageFormat;
    
    private readonly VkGraphicsDevice _device;
    private VkSurface _surface;
    private Extent2D _extent;
    private readonly ILogger? _logger;
    
    private readonly SwapchainKHR _swapchain;
    private readonly KhrSwapchain _khrSwapchain;

    private readonly Image[] _swapchainImages;
    private readonly ImageView[] _swapchainImageViews;
    private readonly Format _imageFormat;
    
    private readonly AllocatedImage[] _depthImages;
    private readonly ImageView[] _depthImageViews;
    private readonly Format _depthFormat;
    
    private readonly Fence[] _imagesInFlight;
    private readonly Fence[] _inFlightFences;
    private readonly Semaphore[] _imageAvailableSemaphores;
    private readonly Semaphore[] _renderFinishedSemaphores;
    
    public readonly Framebuffer[] Framebuffers;
    public RenderPass RenderPass;
    
    private int _currentFrame = 0;
    
    public VkSwapchain(ILogger? logger, VkGraphicsDevice device, VkSurface surface, Extent2D windowExtent, SwapchainKHR? oldSwapchain = null)
    {
        _device = device;
        _extent = windowExtent;
        _surface = surface;
        _logger = logger;
        
        var (capabilities, formats, presentModes) = surface.GetSwapchainSupport(_device.PhysicalDevice);
        var presentMode = PresentModeKHR.FifoKhr;
        if (presentModes.Contains(PresentModeKHR.FifoRelaxedKhr)) presentMode = PresentModeKHR.FifoRelaxedKhr;
        var format = SelectFormat(formats);
        var extent = ValidateExtent(windowExtent, capabilities);
        var imageCount = capabilities.MinImageCount + 1;
        if (capabilities.MaxImageCount > 0 && imageCount > capabilities.MaxImageCount)
            imageCount = capabilities.MaxImageCount;

        var createInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = surface,
            MinImageCount = imageCount,
            ImageFormat = format.Format,
            ImageColorSpace = format.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            PreTransform = capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            ImageSharingMode = SharingMode.Exclusive,
            QueueFamilyIndexCount = 0,
            PQueueFamilyIndices = null,
            Clipped = true,
            OldSwapchain = oldSwapchain??default
        };
        if(!vk.TryGetDeviceExtension(_device.Instance, _device.Device, out _khrSwapchain))
            _logger?.LogError("Device extension not found: {Extension}", KhrSwapchain.ExtensionName);
        _khrSwapchain.CreateSwapchain(_device.Device, createInfo, null, out _swapchain).Validate(_logger);
        
        _khrSwapchain.GetSwapchainImages(_device.Device, _swapchain, &imageCount, null).Validate(_logger);
        _swapchainImages = new Image[imageCount];
        fixed(Image* pSwapchainImages = _swapchainImages)
            _khrSwapchain.GetSwapchainImages(_device.Device, _swapchain, &imageCount, pSwapchainImages).Validate(_logger);

        _imageFormat = format.Format;
        _extent = extent;

        _swapchainImageViews = new ImageView[imageCount];
        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            ViewType = ImageViewType.Type2D,
            Format = _imageFormat,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };
        for (var i = 0; i < imageCount; i++)
        {
            viewInfo.Image = _swapchainImages[i];
            vk.CreateImageView(_device.Device, viewInfo, null, out var imageView).Validate(_logger);
            _swapchainImageViews[i] = imageView;
        }
        
        _depthFormat = device.FindFormat(new[] {Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint},
            ImageTiling.Optimal,
            FormatFeatureFlags.DepthStencilAttachmentBit);
        _depthImages = new AllocatedImage[imageCount];
        _depthImageViews = new ImageView[imageCount];

        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D(_extent.Width, _extent.Height, 1),
            MipLevels = 1,
            ArrayLayers = 1,
            Format = _depthFormat,
            Tiling = ImageTiling.Optimal,
            InitialLayout = ImageLayout.Undefined,
            Usage = ImageUsageFlags.DepthStencilAttachmentBit,
            Samples = SampleCountFlags.Count1Bit,
            SharingMode = SharingMode.Exclusive,
            Flags = 0
        };
        viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            ViewType = ImageViewType.Type2D,
            Format = _depthFormat,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.DepthBit, 0, 1, 0, 1)
        };
        for (var i = 0; i < imageCount; i++)
        {
            var image = _device.CreateImage(imageInfo, MemoryPropertyFlags.DeviceLocalBit);
            _depthImages[i] = image;
            viewInfo.Image = image.Image;
            vk.CreateImageView(_device.Device, viewInfo, null, out var view).Validate(_logger);
            _depthImageViews[i] = view;
        }
        
        _imagesInFlight = new Fence[ImageCount];
        _inFlightFences = new Fence[MaxImagesInFlight];
        _imageAvailableSemaphores = new Semaphore[MaxImagesInFlight];
        _renderFinishedSemaphores = new Semaphore[MaxImagesInFlight];

        var semaphoreInfo = new SemaphoreCreateInfo {SType = StructureType.SemaphoreCreateInfo};
        var fenceInfo = new FenceCreateInfo
            {SType = StructureType.FenceCreateInfo, Flags = FenceCreateFlags.SignaledBit};
        for (var i = 0; i < MaxImagesInFlight; i++)
        {
            vk.CreateSemaphore(_device.Device, semaphoreInfo, null, out var availableSemaphore).Validate(_logger);
            vk.CreateSemaphore(_device.Device, semaphoreInfo, null, out var finishedSemaphore).Validate(_logger);
            vk.CreateFence(_device.Device, fenceInfo, null, out var fence).Validate(_logger);
            _imageAvailableSemaphores[i] = availableSemaphore;
            _renderFinishedSemaphores[i] = finishedSemaphore;
            _inFlightFences[i] = fence;
        }
        
         //renderPass
        var depthAttachment = new AttachmentDescription
        {
            Format = _depthFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
        };

        var depthAttachmentRef = new AttachmentReference
        {
            Attachment = 1,
            Layout = ImageLayout.DepthStencilAttachmentOptimal
        };

        var colorAttachment = new AttachmentDescription
        {
            Format = _imageFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr
        };

        var colorAttachmentRef = new AttachmentReference
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        };

        var subPass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef,
            PDepthStencilAttachment = &depthAttachmentRef
        };

        var dependency = new SubpassDependency
        {
            SrcSubpass = Vk.SubpassExternal,
            SrcAccessMask = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            DstSubpass = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
        };
        var attachments = stackalloc[] {colorAttachment, depthAttachment};
        var renderPassInfo = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 2,
            PAttachments = attachments,
            SubpassCount = 1,
            PSubpasses = &subPass,
            DependencyCount = 1,
            PDependencies = &dependency
        };
        vk.CreateRenderPass(_device.Device, renderPassInfo, null, out RenderPass).Validate(_logger);
        
        //framebuffers
        Framebuffers = new Framebuffer[ImageCount];
        for (var i = 0; i < ImageCount; i++)
            Framebuffers[i] = CreateFramebuffer(i);
    }
    
    private Framebuffer CreateFramebuffer(int i)
    {
        var attachments = stackalloc[] {_swapchainImageViews[i], _depthImageViews[i]};
        var createInfo = new FramebufferCreateInfo
        {
            SType = StructureType.FramebufferCreateInfo,
            RenderPass = RenderPass,
            AttachmentCount = 2,
            PAttachments = attachments,
            Width = _extent.Width,
            Height = _extent.Height,
            Layers = 1
        };
        vk.CreateFramebuffer(_device.Device, createInfo, null, out var framebuffer).Validate(_logger);
        return framebuffer;
    }

    public Result AcquireNextImage(ref uint imageIndex)
    {
        _device.WaitForFence(_inFlightFences[_currentFrame]);
        return _khrSwapchain.AcquireNextImage(_device.Device, _swapchain, ulong.MaxValue,
            _imageAvailableSemaphores[_currentFrame],
            default, ref imageIndex);
    }
    
    public Result SubmitCommandBuffers(VkCommandBuffer cmd, uint imageIndex)
    {
        if (_imagesInFlight[imageIndex].Handle != 0) _device.WaitForFence(_imagesInFlight[imageIndex]);
        _imagesInFlight[imageIndex] = _imagesInFlight[_currentFrame];
        var waitSemaphores = _imageAvailableSemaphores[_currentFrame];
        var signalSemaphores = _renderFinishedSemaphores[_currentFrame];
        var waitStages = PipelineStageFlags.ColorAttachmentOutputBit;
        var current = (CommandBuffer) cmd;
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSemaphores,
            PWaitDstStageMask = &waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &current,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &signalSemaphores
        };
        _device.ResetFence(_inFlightFences[_currentFrame]);
        _device.SubmitMainQueue(submitInfo, _inFlightFences[_currentFrame]);
        var result = QueuePresent(signalSemaphores, imageIndex);
        _currentFrame = (_currentFrame + 1) % MaxImagesInFlight;
        return result;
    }
    
    private Result QueuePresent(Semaphore waitSemaphore, uint imageIndex)
    {
        var swapchain = _swapchain;
        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSemaphore,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &imageIndex
        };
        return _khrSwapchain.QueuePresent(_device.MainQueue, presentInfo);
    }

    public static implicit operator SwapchainKHR(VkSwapchain s) => s._swapchain;
    
    private static SurfaceFormatKHR SelectFormat(SurfaceFormatKHR[] formats)
    {
        foreach (var format in formats)
            if (format is {Format: Format.B8G8R8A8Srgb, ColorSpace: ColorSpaceKHR.SpaceSrgbNonlinearKhr})
                return format;

        return formats[0];
    }
    
    private static Extent2D ValidateExtent(Extent2D extent, SurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue) return capabilities.CurrentExtent;
        var actualExtent = extent;
        actualExtent.Width = Math.Max(capabilities.MinImageExtent.Width,
            Math.Min(capabilities.MaxImageExtent.Width, actualExtent.Width));
        actualExtent.Height = Math.Max(capabilities.MinImageExtent.Height,
            Math.Min(capabilities.MaxImageExtent.Height, actualExtent.Height));
        return actualExtent;
    }

    public void Dispose()
    {
        _khrSwapchain.DestroySwapchain(_device.Device, _swapchain, null);
        _khrSwapchain.Dispose();

        for (var i = 0; i < _swapchainImages.Length; i++)
        {
            vk.DestroyImage(_device.Device, _swapchainImages[i], null);
            vk.DestroyImageView(_device.Device, _swapchainImageViews[i], null);
            _depthImages[i].Allocation.Dispose();
            vk.DestroyImage(_device.Device, _depthImages[i].Image, null);
            vk.DestroyImageView(_device.Device, _depthImageViews[i], null);
        }
        Array.Clear(_swapchainImages);
        Array.Clear(_swapchainImageViews);
        Array.Clear(_depthImageViews);
        Array.Clear(_depthImages);
        
        for (var i = 0; i < MaxImagesInFlight; i++)
        {
            vk.DestroyFence(_device.Device, _inFlightFences[i], null);
            vk.DestroySemaphore(_device.Device, _imageAvailableSemaphores[i], null);
            vk.DestroySemaphore(_device.Device, _renderFinishedSemaphores[i], null);
        }
        Array.Clear(_inFlightFences);
        Array.Clear(_imageAvailableSemaphores);
        Array.Clear(_renderFinishedSemaphores);
    }
}
