using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Licht.Vulkan;

public readonly unsafe struct Surface : IDisposable
{
    private readonly Instance _instance;
    private readonly KhrSurface _khrSurface;
    private readonly SurfaceKHR _surface;
    private readonly ulong Handle => _surface.Handle;

    public static implicit operator SurfaceKHR(Surface s) => s._surface;
    public static implicit operator SurfaceKHR*(Surface s) => &s._surface;

    public Surface(Instance instance, IVkSurfaceSource surfaceSource)
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

    public void Dispose()
    {
        _khrSurface.DestroySurface(_instance, _surface, null);
        _khrSurface.Dispose();
    }
}