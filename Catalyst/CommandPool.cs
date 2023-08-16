using Catalyst.Tools;
using Silk.NET.Vulkan;

namespace Catalyst;

public readonly unsafe struct CommandPool : IDisposable
{
    public readonly Silk.NET.Vulkan.CommandPool VkCommandPool;
    public ulong Handle => VkCommandPool.Handle;
    private readonly Device _device;
    
    public CommandPool(Device device,
        CommandPoolCreateFlags flags = CommandPoolCreateFlags.TransientBit | CommandPoolCreateFlags.ResetCommandBufferBit)
    {
        _device = device;
        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = device.MainQueueIndex,
            Flags = flags
        };
        vk.CreateCommandPool(_device, poolInfo, null, out VkCommandPool).Validate();
    }

    public void Reset(CommandPoolResetFlags flags = CommandPoolResetFlags.None) => vk.ResetCommandPool(_device, VkCommandPool, flags);

    public static implicit operator Silk.NET.Vulkan.CommandPool(CommandPool c) => c.VkCommandPool;

    public void Dispose() => vk.DestroyCommandPool(_device, VkCommandPool, null);
}