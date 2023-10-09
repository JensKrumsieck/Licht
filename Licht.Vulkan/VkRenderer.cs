﻿using Licht.Core.Graphics;
using Licht.Vulkan.Extensions;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;

namespace Licht.Vulkan;

public class VkRenderer : IRenderer, IDisposable
{
    private readonly ILogger? _logger;
    private readonly VkGraphicsDevice _device;
    private readonly VkSurface _surface;
    private readonly IWindowProvider _windowProvider;
    private readonly VkCommandBuffer[] _commandBuffers;
    
    private uint _currentImageIndex;
    private int _currentFrameIndex;
    
    private VkSwapchain? _swapchain;
    
    private readonly ClearValue[] _clearValues = {
        new() {Color = new ClearColorValue(.01f, .01f, .01f, 1f)},
        new() {DepthStencil = new ClearDepthStencilValue(1f, 0u)}
    };
    
    public VkCommandBuffer CurrentCommandBuffer => _commandBuffers[_currentFrameIndex];
    
    public VkRenderer(VkGraphicsDevice device, IWindowProvider windowProvider, VkSurface surface, ILogger? logger = null)
    {
        _logger = logger;
        _device = device;
        _surface = surface;
        _windowProvider = windowProvider;
        RecreateSwapchain();
        _commandBuffers = _device.AllocateCommandBuffers((uint) _swapchain!.ImageCount);
    }

    private void RecreateSwapchain()
    {
        var extent = new Extent2D((uint) _windowProvider.Window.FramebufferSize.X, (uint) _windowProvider.Window.FramebufferSize.Y);
        while (extent.Width == 0 || extent.Height == 0)
        {
            extent = new Extent2D((uint) _windowProvider.Window.FramebufferSize.X, (uint) _windowProvider.Window.FramebufferSize.Y);
            _windowProvider.Window.DoEvents();
        }
        _device.WaitIdle();
        if (_swapchain is null) _swapchain = new VkSwapchain(_logger, _device, _surface, extent);
        else
        {
            var oldSwapchain = _swapchain;
            _swapchain = new VkSwapchain(_logger, _device, _surface, extent, oldSwapchain);
            oldSwapchain.Dispose();
        }
    }
    
    public ICommandList BeginFrame()
    {
        var result = _swapchain?.AcquireNextImage(ref _currentImageIndex);
        if (result == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapchain();
            return default!;
        }
        if(result != Result.Success && result != Result.SuboptimalKhr) result?.Validate(_logger);
        var cmd = CurrentCommandBuffer;
        cmd.Begin().Validate(_logger);
        return cmd;
    }
    
    public unsafe void BeginRenderPass(ICommandList cmd)
    {
        var commandBuffer = (VkCommandBuffer) cmd;
        fixed (ClearValue* pClearValues = _clearValues)
        {
            var beginInfo = new RenderPassBeginInfo
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = _swapchain!.RenderPass,
                Framebuffer = _swapchain.Framebuffers[_currentImageIndex],
                RenderArea = {Offset = {X = 0, Y = 0}, Extent = _swapchain.Extent},
                ClearValueCount = (uint) _clearValues.Length,
                PClearValues = pClearValues
            };
            commandBuffer.BeginRenderPass(beginInfo);
            var viewport = new Viewport(0, 0, _swapchain.Extent.Width, _swapchain.Extent.Height, 0, 1);
            var scissor = new Rect2D {Offset = {X = 0, Y = 0}, Extent = _swapchain.Extent};
            commandBuffer.SetViewport(viewport);
            commandBuffer.SetScissor(scissor);
        }
    }

    public void EndRenderPass(ICommandList cmd) => ((VkCommandBuffer)cmd).EndRenderPass();
    
    public void EndFrame()
    {
        CurrentCommandBuffer.End().Validate(_logger);
        if (_swapchain!.SubmitCommandBuffers(CurrentCommandBuffer, _currentImageIndex) 
            is Result.ErrorOutOfDateKhr
            or Result.SuboptimalKhr)
            RecreateSwapchain();
        _currentFrameIndex = (_currentFrameIndex + 1) % VkSwapchain.MaxImagesInFlight;
    }
    
    public void Dispose()
    {
        _surface.Dispose();
        _device.Dispose();
        _swapchain?.Dispose();
        
        GC.SuppressFinalize(this);
    }
}