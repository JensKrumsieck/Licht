using Catalyst.Allocation;
using Catalyst.Tools;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Catalyst;

public readonly unsafe struct Swapchain : IDisposable
{
    public const int MaxImagesInFlight = 2;
    private readonly Device _device;

    public readonly SwapchainKHR VkSwapchain;
    public int ImageCount => _swapchainImages.Length;

    private readonly Image[] _swapchainImages;
    private readonly ImageView[] _swapchainImageViews;
    private readonly Format _imageFormat;

    private readonly AllocatedImage[] _depthImages;
    private readonly ImageView[] _depthImageViews;
    private readonly Format _depthFormat;
    
    private readonly Extent2D _extent;
    
    private readonly KhrSwapchain _khrSwapchain;
    
    public Swapchain(Device device, Surface surface, IAllocator allocator, Extent2D windowExtent, Swapchain? OldSwapchain = null)
    {
        _device = device;
        //create swapchain
        var swapchainSupport = surface.GetSwapchainSupport(_device.PhysicalDevice);
        var surfaceFormat = SelectFormat(swapchainSupport.Formats);
        var presentMode = PresentModeKHR.FifoKhr;
        if (swapchainSupport.PresentModes.Contains(PresentModeKHR.MailboxKhr)) presentMode = PresentModeKHR.MailboxKhr;
        var extent = ValidateExtent(windowExtent, swapchainSupport.Capabilities);
        var imageCount = swapchainSupport.Capabilities.MinImageCount + 1;
        if (swapchainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapchainSupport.Capabilities.MaxImageCount)
            imageCount = swapchainSupport.Capabilities.MaxImageCount;
        var createInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = surface,
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
            OldSwapchain = OldSwapchain ?? default
        };

        if (!vk.TryGetDeviceExtension(vk.CurrentInstance!.Value, _device, out _khrSwapchain))
            throw new Exception($"Could not get device extension {KhrSwapchain.ExtensionName}");
        _khrSwapchain.CreateSwapchain(_device, createInfo, null, out VkSwapchain);

        //get images
        _khrSwapchain.GetSwapchainImages(_device, VkSwapchain, &imageCount, null);
        _swapchainImages = new Image[imageCount];
        fixed (Image* pSwapchainImages = _swapchainImages)
            _khrSwapchain.GetSwapchainImages(_device, VkSwapchain, &imageCount, pSwapchainImages);
        
        _imageFormat = surfaceFormat.Format;
        _extent = extent;

        //create views
        _swapchainImageViews = new ImageView[imageCount];
        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            ViewType = ImageViewType.Type2D,
            Format = _imageFormat,
            SubresourceRange = new(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };
        for (var i = 0; i < imageCount; i++)
        {
            viewInfo.Image = _swapchainImages[i];
            vk.CreateImageView(_device, viewInfo, null, out var imageView).Validate();
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
            var image = _device.CreateImage(allocator, imageInfo, MemoryPropertyFlags.DeviceLocalBit);
            _depthImages[i] = image;
            viewInfo.Image = image.Image;
            vk.CreateImageView(_device, viewInfo, null, out var view).Validate();
            _depthImageViews[i] = view;
        }
    }

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
        for (var i = 0; i < ImageCount; i++)
        {
            vk.DestroyImageView(_device, _swapchainImageViews[i], null);
            vk.DestroyImageView(_device, _depthImageViews[i], null);
            vk.DestroyImage(_device, _depthImages[i].Image, null);
            _depthImages[i].Allocation.Dispose();
        }
        Array.Clear(_depthImages);
        Array.Clear(_depthImageViews);
        Array.Clear(_swapchainImageViews);
        
        _khrSwapchain.DestroySwapchain(_device, VkSwapchain, null);
        Array.Clear(_swapchainImages);
        _khrSwapchain.Dispose();
    }
}