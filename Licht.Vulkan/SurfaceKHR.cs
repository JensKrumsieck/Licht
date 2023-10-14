using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Licht.Vulkan;

public readonly unsafe struct SurfaceKHR : IDisposable
{
    private readonly Instance _instance;
    private readonly KhrSurface _khrSurface;
    private readonly Silk.NET.Vulkan.SurfaceKHR _surface;
    private readonly ulong Handle => _surface.Handle;

    public static implicit operator Silk.NET.Vulkan.SurfaceKHR(SurfaceKHR s) => s._surface;
    public static implicit operator Silk.NET.Vulkan.SurfaceKHR*(SurfaceKHR s) => &s._surface;

    public SurfaceKHR(Instance instance, IVkSurfaceSource surfaceSource)
    {
        _instance = instance;
        vk.TryGetInstanceExtension(_instance, out _khrSurface);
        _surface = surfaceSource.VkSurface!
            .Create<AllocationCallbacks>(new VkHandle((nint)_instance.Handle), null)
            .ToSurface();
    }
    
    public (SurfaceCapabilitiesKHR capabilities, SurfaceFormatKHR[] formats, PresentModeKHR[] presentModes) GetSwapchainSupport(Silk.NET.Vulkan.PhysicalDevice physicalDevice)
    {
        _khrSurface.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, _surface, out var capabilities);
        var count = 0u;
        _khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, _surface, &count, null);
        var formats = new SurfaceFormatKHR[count];
        _khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, _surface, &count, formats);

        _khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, _surface, &count, null);
        var presentModes = new PresentModeKHR[count];
        _khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, _surface, &count, presentModes);
        return (capabilities, formats, presentModes);
    }

    public static SurfaceFormatKHR SelectFormat(SurfaceFormatKHR[] formats, Format desiredFormat, ColorSpaceKHR desiredColorSpace)
    {
        foreach (var format in formats)
            if (format.Format == desiredFormat && format.ColorSpace == desiredColorSpace)
                return format;

        return formats[0];
    }

    public static Extent2D ValidateExtent(Extent2D extent, SurfaceCapabilitiesKHR capabilities)
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
        _khrSurface.DestroySurface(_instance, _surface, null);
        _khrSurface.Dispose();
    }
}