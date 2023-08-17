using Catalyst.Allocation;
using Catalyst.Tools;
using Silk.NET.Core.Contexts;
using Silk.NET.Vulkan;

namespace Catalyst.Engine.Graphics;

public sealed class GraphicsDevice : IDisposable
{
    private readonly Instance _instance;
    private readonly PhysicalDevice _physicalDevice;
    private readonly Device _device;
    private readonly CommandPool _commandPool;
    private readonly Surface _surface;
    private readonly IAllocator _allocator;

    public Device Device => _device;
    public Surface Surface => _surface;
    public IAllocator Allocator => _allocator;
    public CommandPool CommandPool => _commandPool;

    public GraphicsDevice(IVkSurfaceSource window)
    {
        _instance = new Instance();
        _physicalDevice = _instance.SelectPhysicalDevice(SelectorTools.DefaultGpuSelector);
        _device = new Device(_physicalDevice);
        _commandPool = new CommandPool(_device);
        _surface = new Surface(window, _instance);
        _allocator = new PassthroughAllocator(_device);
    }

    public SwapchainSupport GetSwapchainSupport() => _surface.GetSwapchainSupport(_physicalDevice);
    public void WaitIdle() => _device.WaitIdle();

    public CommandBuffer[] AllocateCommandBuffers(uint count) => _device.AllocateCommandBuffers(count, _commandPool);
    public void FreeCommandBuffers(CommandBuffer[] commandBuffers) => _device.FreeCommandBuffers(commandBuffers, _commandPool);

    public Sampler CreateSampler(SamplerCreateInfo info) => _device.CreateSampler(info);
    public void DestroySampler(Sampler sampler) => _device.DestroySampler(sampler);
    public void Dispose()
    {
        _allocator.Dispose();
        _commandPool.Dispose();
        _device.Dispose();
        _surface.Dispose();
        _instance.Dispose();
    }
}