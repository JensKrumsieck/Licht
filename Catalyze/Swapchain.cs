using Catalyze.Allocation;
using Catalyze.Tools;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Catalyze;

public unsafe struct Swapchain : IDisposable, IConvertibleTo<SwapchainKHR>
{
    private readonly GraphicsDevice _device;

    public const int MaxImagesInFlight = 2;
    public readonly SwapchainKHR VkSwapchain;
    public int ImageCount => _swapchainImages.Length;
    public Extent2D Extent => _extent;
    public Format DepthFormat => _depthFormat;
    public Format ImageFormat => _imageImageFormat;

    private readonly Image[] _swapchainImages;
    private readonly ImageView[] _swapchainImageViews;
    private readonly Format _imageImageFormat;

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
    private readonly Extent2D _extent;

    private readonly KhrSwapchain _khrSwapchain;

    public Swapchain(GraphicsDevice device, Extent2D windowExtent,
                     Swapchain? oldSwapchain = null)
    {
        _device = device;
        //create swapchain
        var swapchainSupport = device.GetSwapchainSupport(_device.VkPhysicalDevice);
        var surfaceFormat = SelectFormat(swapchainSupport.Formats);
        var presentMode = PresentModeKHR.FifoKhr;
        if (swapchainSupport.PresentModes.Contains(PresentModeKHR.FifoRelaxedKhr)) presentMode = PresentModeKHR.FifoRelaxedKhr;
        var extent = ValidateExtent(windowExtent, swapchainSupport.Capabilities);
        var imageCount = swapchainSupport.Capabilities.MinImageCount + 1;
        if (swapchainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapchainSupport.Capabilities.MaxImageCount)
            imageCount = swapchainSupport.Capabilities.MaxImageCount;
        var createInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = _device.VkSurface,
            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            PreTransform = swapchainSupport.Capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            ImageSharingMode = SharingMode.Exclusive,
            QueueFamilyIndexCount = 0,
            PQueueFamilyIndices = null,
            Clipped = true,
            OldSwapchain = oldSwapchain ?? default
        };

        if (!vk.TryGetDeviceExtension(vk.CurrentInstance!.Value, _device.VkDevice, out _khrSwapchain))
            throw new Exception($"Could not get device extension {KhrSwapchain.ExtensionName}");
        _khrSwapchain.CreateSwapchain(_device.VkDevice, createInfo, null, out VkSwapchain);

        //get images
        _khrSwapchain.GetSwapchainImages(_device.VkDevice, VkSwapchain, &imageCount, null);
        _swapchainImages = new Image[imageCount];
        fixed (Image* pSwapchainImages = _swapchainImages)
            _khrSwapchain.GetSwapchainImages(_device.VkDevice, VkSwapchain, &imageCount, pSwapchainImages);

        _imageImageFormat = surfaceFormat.Format;
        _extent = extent;

        //create views
        _swapchainImageViews = new ImageView[imageCount];
        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            ViewType = ImageViewType.Type2D,
            Format = _imageImageFormat,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };
        for (var i = 0; i < imageCount; i++)
        {
            viewInfo.Image = _swapchainImages[i];
            vk.CreateImageView(_device.VkDevice, viewInfo, null, out var imageView).Validate();
            _swapchainImageViews[i] = imageView;
        }

        //depth resources
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
            vk.CreateImageView(_device.VkDevice, viewInfo, null, out var view).Validate();
            _depthImageViews[i] = view;
        }

        //sync resources
        _imagesInFlight = new Fence[ImageCount];
        _inFlightFences = new Fence[MaxImagesInFlight];
        _imageAvailableSemaphores = new Semaphore[MaxImagesInFlight];
        _renderFinishedSemaphores = new Semaphore[MaxImagesInFlight];

        var semaphoreInfo = new SemaphoreCreateInfo {SType = StructureType.SemaphoreCreateInfo};
        var fenceInfo = new FenceCreateInfo
            {SType = StructureType.FenceCreateInfo, Flags = FenceCreateFlags.SignaledBit};
        for (var i = 0; i < MaxImagesInFlight; i++)
        {
            vk.CreateSemaphore(_device.VkDevice, semaphoreInfo, null, out var availableSemaphore).Validate();
            vk.CreateSemaphore(_device.VkDevice, semaphoreInfo, null, out var finishedSemaphore).Validate();
            vk.CreateFence(_device.VkDevice, fenceInfo, null, out var fence).Validate();
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
            Format = _imageImageFormat,
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
        vk.CreateRenderPass(_device.VkDevice, renderPassInfo, null, out RenderPass).Validate();
        
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
        vk.CreateFramebuffer(_device.VkDevice, createInfo, null, out var framebuffer).Validate();
        return framebuffer;
    }

    public Result AcquireNextImage(ref uint imageIndex)
    {
        _device.WaitForFence(_inFlightFences[_currentFrame]);
        return _khrSwapchain.AcquireNextImage(_device.VkDevice,
                                              VkSwapchain,
                                              ulong.MaxValue,
                                              _imageAvailableSemaphores[_currentFrame],
                                              default,
                                              ref imageIndex);
    }

    public Result SubmitCommandBuffers(CommandBuffer cmd, uint imageIndex)
    {
        if (_imagesInFlight[imageIndex].Handle != 0) _device.WaitForFence(_imagesInFlight[imageIndex]);
        _imagesInFlight[imageIndex] = _imagesInFlight[_currentFrame];
        var waitSemaphores = _imageAvailableSemaphores[_currentFrame];
        var signalSemaphores = _renderFinishedSemaphores[_currentFrame];
        var waitStages = PipelineStageFlags.ColorAttachmentOutputBit;
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSemaphores,
            PWaitDstStageMask = &waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &cmd.VkCommandBuffer,
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
        var swapchain = VkSwapchain;
        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSemaphore,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &imageIndex
        };
        return _khrSwapchain.QueuePresent(_device.VkMainDeviceQueue, presentInfo);
    }
    public (ImageView Color, ImageView Depth) GetAttachments(int index) =>
        (_swapchainImageViews[index], _depthImageViews[index]);

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

    public static implicit operator SwapchainKHR(Swapchain s) => s.VkSwapchain;

    public void Dispose()
    {
        vk.DestroyRenderPass(_device.VkDevice, RenderPass, null);
        for (var i = 0; i < ImageCount; i++)
        {
            vk.DestroyImageView(_device.VkDevice, _swapchainImageViews[i], null);
            vk.DestroyImageView(_device.VkDevice, _depthImageViews[i], null);
            vk.DestroyImage(_device.VkDevice, _depthImages[i].Image, null);
            _depthImages[i].Allocation.Dispose();
            vk.DestroyFence(_device.VkDevice, _imagesInFlight[i], null);
            vk.DestroyFramebuffer(_device.VkDevice, Framebuffers[i], null);
        }
        Array.Clear(_depthImages);
        Array.Clear(_depthImageViews);
        Array.Clear(_swapchainImageViews);
        Array.Clear(_imagesInFlight);
        Array.Clear(Framebuffers);

        for (var i = 0; i < MaxImagesInFlight; i++)
        {
            vk.DestroyFence(_device.VkDevice, _inFlightFences[i], null);
            vk.DestroySemaphore(_device.VkDevice, _imageAvailableSemaphores[i], null);
            vk.DestroySemaphore(_device.VkDevice, _renderFinishedSemaphores[i], null);
        }
        Array.Clear(_inFlightFences);
        Array.Clear(_imageAvailableSemaphores);
        Array.Clear(_renderFinishedSemaphores);
        
        _khrSwapchain.DestroySwapchain(_device.VkDevice, VkSwapchain, null);
        Array.Clear(_swapchainImages);
        _khrSwapchain.Dispose();
    }

    public SwapchainKHR Convert() => VkSwapchain;
}