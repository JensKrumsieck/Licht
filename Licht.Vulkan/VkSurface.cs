using Microsoft.Extensions.Logging;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Licht.Vulkan;

public sealed unsafe class VkSurface : IDisposable
{
    private readonly KhrSurface _khrSurface;
    private readonly SurfaceKHR _surface;
    public VkSurface(IVkSurfaceSource surfaceProvider, ILogger? logger = null)
    {
        if(!vk.TryGetInstanceExtension(instance, out _khrSurface)) 
            logger?.LogError($"Could not find instance extension {KhrSurface.ExtensionName}");
        _surface = surfaceProvider.VkSurface!
            .Create<AllocationCallbacks>(new VkHandle(instance.Handle), null)
            .ToSurface();
    }
    
    public (SurfaceCapabilitiesKHR capabilities, SurfaceFormatKHR[] formats, PresentModeKHR[] presentModes) GetSwapchainSupport(PhysicalDevice physicalDevice)
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
        _khrSurface.DestroySurface(instance, _surface, null);
        _khrSurface.Dispose();
    }
}
