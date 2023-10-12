using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Licht.Vulkan;

public sealed class VkSurface : IDisposable
{
    private readonly Surface _surface;
    public VkSurface(VkGraphicsDevice device, IWindow surfaceProvider) => _surface = device.Instance.CreateSurface(surfaceProvider);

    public (SurfaceCapabilitiesKHR capabilities, SurfaceFormatKHR[] formats, PresentModeKHR[] presentModes) GetSwapchainSupport(PhysicalDevice physicalDevice)
    => _surface.GetSwapchainSupport(physicalDevice);
    
    public static implicit operator SurfaceKHR(VkSurface s) => s._surface;
    
    public void Dispose() => _surface.Dispose();
}
