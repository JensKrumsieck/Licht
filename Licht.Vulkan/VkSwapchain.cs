﻿using Licht.Vulkan.Extensions;
using Licht.Vulkan.Memory;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;

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
    private readonly Extent2D _extent;
    private readonly ILogger? _logger;
    
    private readonly SwapchainKHR _swapchain;

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
    
    public VkSwapchain(ILogger? logger, VkGraphicsDevice device, VkSurface surface, Extent2D windowExtent, Silk.NET.Vulkan.SwapchainKHR? oldSwapchain = null)
    {
        _device = device;
        _extent = windowExtent;
        _surface = surface;
        _logger = logger;
        
        var (capabilities, formats, presentModes) = surface.GetSwapchainSupport(_device.PhysicalDevice);
        var presentMode = PresentModeKHR.FifoKhr;
        if (presentModes.Contains(PresentModeKHR.FifoRelaxedKhr)) presentMode = PresentModeKHR.FifoRelaxedKhr;
        var format = SurfaceKHR.SelectFormat(formats, Format.B8G8R8A8Srgb, ColorSpaceKHR.SpaceSrgbNonlinearKhr);
        var extent = SurfaceKHR.ValidateExtent(windowExtent, capabilities);
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
        _swapchain = new SwapchainKHR(_device.Instance, device.Device, createInfo);
        _swapchain.GetSwapchainImages(&imageCount, out _swapchainImages).Validate(_logger);
        
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
            _swapchainImageViews[i] = new ImageView(_device.Device, viewInfo);
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
            _depthImageViews[i] = new ImageView(_device.Device, viewInfo);
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
            _imageAvailableSemaphores[i] = new Semaphore(_device.Device, semaphoreInfo);
            _renderFinishedSemaphores[i] = new Semaphore(_device.Device, semaphoreInfo);
            _inFlightFences[i] = new Fence(_device.Device, fenceInfo);
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
        RenderPass = new RenderPass(_device, renderPassInfo);
        
        //framebuffers
        Framebuffers = new Framebuffer[ImageCount];
        for (var i = 0; i < ImageCount; i++)
            Framebuffers[i] = CreateFramebuffer(i);
    }
    
    private Framebuffer CreateFramebuffer(int i)
    {
        var attachments = stackalloc Silk.NET.Vulkan.ImageView[] {_swapchainImageViews[i], _depthImageViews[i]};
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
        return new Framebuffer(_device, createInfo);
    }

    public Result AcquireNextImage(ref uint imageIndex)
    {
        _device.WaitForFence(_inFlightFences[_currentFrame]);
        return _swapchain.AcquireNextImage(ulong.MaxValue, _imageAvailableSemaphores[_currentFrame], default, ref imageIndex);
    }
    
    public Result SubmitCommandBuffers(CommandBuffer cmd, uint imageIndex)
    {
        if (_imagesInFlight[imageIndex].Handle != 0) _device.WaitForFence(_imagesInFlight[imageIndex]);
        _imagesInFlight[imageIndex] = _imagesInFlight[_currentFrame];
        var waitSemaphores = _imageAvailableSemaphores[_currentFrame];
        var signalSemaphores = _renderFinishedSemaphores[_currentFrame];
        var waitStages = PipelineStageFlags.ColorAttachmentOutputBit;
        var current = (Silk.NET.Vulkan.CommandBuffer) cmd;
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = &waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &current,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores
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
            PWaitSemaphores = waitSemaphore,
            SwapchainCount = 1,
            PSwapchains = swapchain,
            PImageIndices = &imageIndex
        };
        return _swapchain.QueuePresent(_device.MainQueue, presentInfo);
    }

    public static implicit operator Silk.NET.Vulkan.SwapchainKHR(VkSwapchain s) => s._swapchain;

    public void Dispose()
    {
        _device.WaitIdle();
        vk.DestroyRenderPass(_device.Device, RenderPass, null);
        for (var i = 0; i < _swapchainImages.Length; i++)
        {
            _swapchainImageViews[i].Dispose();
            _depthImages[i].Image.Dispose();
            _depthImageViews[i].Dispose();
            _depthImages[i].Allocation.Dispose();
            Framebuffers[i].Dispose();
        }
        Array.Clear(_swapchainImages);
        Array.Clear(_swapchainImageViews);
        Array.Clear(_depthImageViews);
        Array.Clear(_depthImages);
        Array.Clear(_imagesInFlight);
        Array.Clear(Framebuffers);
        
        for (var i = 0; i < MaxImagesInFlight; i++)
        {
            _inFlightFences[i].Dispose();
            _imageAvailableSemaphores[i].Dispose();
            _renderFinishedSemaphores[i].Dispose();
        }
        Array.Clear(_inFlightFences);
        Array.Clear(_imageAvailableSemaphores);
        Array.Clear(_renderFinishedSemaphores);

        _swapchain.Dispose();
    }
}
