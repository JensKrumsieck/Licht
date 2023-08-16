using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Catalyst.Engine.Graphics;

public sealed class Renderer : IDisposable
{
    private Swapchain _swapchain;
    private readonly IWindow _window;
    private readonly GraphicsDevice _device;
    
    public Renderer(GraphicsDevice device, IWindow window)
    {
        _window = window;
        _device = device;
        RecreateSwapchain();
    }

    private void RecreateSwapchain()
    {
        var extent = new Extent2D((uint)_window.FramebufferSize.X, (uint)_window.FramebufferSize.Y);
        while (extent.Width == 0 || extent.Height == 0)
        {
            extent= new Extent2D((uint)_window.FramebufferSize.X, (uint)_window.FramebufferSize.Y);
            _window.DoEvents();
        }
        _device.WaitIdle();
        
        if(_swapchain.VkSwapchain.Handle == 0) _swapchain = new Swapchain(_device.Device, _device.Surface, _device.Allocator, extent);
        else
        {
            var oldSwapchain = _swapchain;
            _swapchain = new Swapchain(_device.Device, _device.Surface, _device.Allocator, extent, oldSwapchain);
            //TODO: Compare Formats
            oldSwapchain.Dispose();
        }
    }

    public void Dispose()
    {
        _swapchain.Dispose();
    }
}