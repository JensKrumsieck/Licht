using Silk.NET.Vulkan;

namespace Licht.Vulkan;

public sealed unsafe class VkSwapchain : IDisposable
{
    private VkGraphicsDevice _device;
    private VkSurface _surface;
    private Extent2D _extent;
    
    public VkSwapchain(VkGraphicsDevice device, VkSurface surface, Extent2D windowExtent, VkSwapchain? oldSwapchain = null)
    {
        _device = device;
        _extent = windowExtent;
    }
    
    public void Dispose(){}
}
