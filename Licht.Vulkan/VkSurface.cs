using Microsoft.Extensions.Logging;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;

namespace Licht.Vulkan;

public sealed unsafe class VkSurface : IDisposable
{
    private readonly VkGraphicsDevice _device;
    private readonly KhrSurface _khrSurface;
    private readonly Silk.NET.Vulkan.SurfaceKHR _surface;
    public VkSurface(VkGraphicsDevice device, IWindow surfaceProvider, ILogger? logger = null)
    {
        _device = device;
        if(!vk.TryGetInstanceExtension(_device.Instance, out _khrSurface)) 
            logger?.LogError($"Could not find instance extension {KhrSurface.ExtensionName}");
        _surface = surfaceProvider.VkSurface!
            .Create<AllocationCallbacks>(new VkHandle((nint)_device.Instance.Handle), null)
            .ToSurface();
        logger?.LogTrace("VkSurface created");
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

    public static implicit operator Silk.NET.Vulkan.SurfaceKHR(VkSurface s) => s._surface;
    
    public void Dispose()
    {
        _khrSurface.DestroySurface(_device.Instance, _surface, null);
        _khrSurface.Dispose();
    }
}
