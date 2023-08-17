using Catalyst.Tools;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Catalyst.Engine.Graphics;

public sealed class Renderer : IDisposable
{
    private Swapchain _swapchain;
    private readonly IWindow _window;
    private readonly GraphicsDevice _device;
    private readonly CommandBuffer[] _commandBuffers;
    
    private uint _currentImageIndex;
    private int _currentFrameIndex;
    
    private readonly ClearValue[] _clearValues = {
        new() {Color = new ClearColorValue(.01f, .01f, .01f, 1f)},
        new() {DepthStencil = new ClearDepthStencilValue(1f, 0u)}
    };
    
    public CommandBuffer CurrentCommandBuffer => _commandBuffers[_currentFrameIndex];
    public RenderPass RenderPass => _swapchain.RenderPass;

    public Renderer(GraphicsDevice device, IWindow window)
    {
        _window = window;
        _device = device;
        RecreateSwapchain();
        _commandBuffers = _device.AllocateCommandBuffers((uint)_swapchain.ImageCount);
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
    
    public CommandBuffer BeginFrame()
    {
        var result = _swapchain.AcquireNextImage(ref _currentImageIndex);
        if (result == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapchain();
            return default;
        }
        if(result != Result.Success && result != Result.SuboptimalKhr) result.Validate();
        var cmd = CurrentCommandBuffer;
        cmd.Begin().Validate();
        return cmd;
    }

    public unsafe void BeginRenderPass(CommandBuffer cmd)
    {
        fixed (ClearValue* pClearValues = _clearValues)
        {
            var beginInfo = new RenderPassBeginInfo
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = _swapchain.RenderPass,
                Framebuffer = _swapchain.Framebuffers[_currentImageIndex],
                RenderArea = {Offset = {X = 0, Y = 0}, Extent = _swapchain.Extent},
                ClearValueCount = (uint) _clearValues.Length,
                PClearValues = pClearValues
            };
            cmd.BeginRenderPass(beginInfo);
        }
        var viewport = new Viewport(0, 0, _swapchain.Extent.Width, _swapchain.Extent.Height, 0, 1);
        var scissor = new Rect2D {Offset = {X = 0, Y = 0}, Extent = _swapchain.Extent};
        cmd.SetViewport(viewport);
        cmd.SetScissor(scissor);
    }

    public void EndRenderPass(CommandBuffer cmd) => cmd.EndRenderPass();

    public void EndFrame()
    {
        var cmd = CurrentCommandBuffer;
        cmd.End().Validate();
        if (_swapchain.SubmitCommandBuffers(cmd, _currentImageIndex) 
            is Result.ErrorOutOfDateKhr
            or Result.SuboptimalKhr)
            RecreateSwapchain();
        _currentFrameIndex = (_currentFrameIndex + 1) % Swapchain.MaxImagesInFlight;
    }
    
    public void Dispose()
    {
        _device.FreeCommandBuffers(_commandBuffers);
        _swapchain.Dispose();
    }
}