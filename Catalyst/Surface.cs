using Catalyst.Tools;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Catalyst;

public record struct SwapchainSupport(
    SurfaceCapabilitiesKHR Capabilities,
    SurfaceFormatKHR[] Formats,
    PresentModeKHR[] PresentModes);

public readonly unsafe struct Surface : IDisposable, IConvertibleTo<SurfaceKHR>
{
    public readonly SurfaceKHR VkSurface;

    private readonly Instance _instance;
    private readonly KhrSurface _khrSurface;
    
    public Surface(IVkSurfaceSource surfaceSource, Instance instance)
    {
        _instance = instance;
        if (!vk.TryGetInstanceExtension(_instance, out _khrSurface))
            throw new Exception($"Could not get instance extension {KhrSurface.ExtensionName}");
        VkSurface = surfaceSource.VkSurface!
                                 .Create<AllocationCallbacks>(new VkHandle(_instance.Handle), null)
                                 .ToSurface();
    }

    public SwapchainSupport GetSwapchainSupport(PhysicalDevice physicalDevice)
    {
        _khrSurface.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, VkSurface, out var capabilities);

        var count = 0u;
        _khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, VkSurface, &count, null);
        var formats = new SurfaceFormatKHR[count];
        _khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, VkSurface, &count, formats);

        _khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, VkSurface, &count, null);
        var presentModes = new PresentModeKHR[count];
        _khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, VkSurface, &count, presentModes);
        return new SwapchainSupport(capabilities, formats, presentModes);
    }

    public static implicit operator SurfaceKHR(Surface s) => s.VkSurface;
    
    public void Dispose()
    {
        _khrSurface.DestroySurface(_instance, VkSurface, null);
        _khrSurface.Dispose();
    }

    public SurfaceKHR Convert() => VkSurface;
}