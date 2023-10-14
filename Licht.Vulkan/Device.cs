﻿using Licht.Vulkan.Memory;
using Silk.NET.Vulkan;

namespace Licht.Vulkan;

public unsafe partial struct Device
{
    public Queue GetQueue(uint familyIndex, uint queueIndex = 0)
    {
        vk.GetDeviceQueue(this, familyIndex, queueIndex, out var queue);
        return queue;
    }
    public void WaitIdle() => vk.DeviceWaitIdle(_device);
    public void WaitForFence(Fence fence) => vk.WaitForFences(_device, 1u, fence, true, ulong.MaxValue);
    public Result ResetFence(Fence fence) => vk.ResetFences(_device, 1, fence);
    
    public CommandBuffer[] AllocateCommandBuffers(uint count, CommandPool pool)
    {
        var commandBuffers = new Silk.NET.Vulkan.CommandBuffer[count];
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = pool,
            CommandBufferCount = count
        };
        fixed (Silk.NET.Vulkan.CommandBuffer* pCommandBuffers = commandBuffers)
            vk.AllocateCommandBuffers(_device, allocInfo, pCommandBuffers);
        var buffers = new CommandBuffer[count];
        for (var i = 0; i < buffers.Length; i++) buffers[i] = commandBuffers[i];
        return buffers;
    }

    public void FreeCommandBuffer(CommandBuffer commandBuffer, CommandPool pool) =>
        vk.FreeCommandBuffers(_device, pool, 1, commandBuffer);

    public void FreeCommandBuffers(CommandBuffer[] commandBuffers, CommandPool pool) =>
        vk.FreeCommandBuffers(_device, pool, (uint)commandBuffers.Length,
            Array.ConvertAll(commandBuffers, cmd => (Silk.NET.Vulkan.CommandBuffer)cmd));

    public AllocatedImage CreateImage(IAllocator allocator, ImageCreateInfo info, MemoryPropertyFlags propertyFlags)
    {
        var image = new Image(this, info);
        var allocInfo = new AllocationCreateInfo { Usage = propertyFlags };
        allocator.AllocateImage(image, allocInfo, out var allocation);
        return new AllocatedImage(image, allocation);
    }

    public AllocatedBuffer CreateBuffer(IAllocator allocator, ulong bufferSize, BufferUsageFlags usageFlags, MemoryPropertyFlags memoryFlags)
    {
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = bufferSize,
            Usage = usageFlags,
            SharingMode = SharingMode.Exclusive
        };
        var buffer = new Buffer(this, bufferInfo);
        var allocInfo = new AllocationCreateInfo { Usage = memoryFlags };
        allocator.AllocateBuffer(buffer, allocInfo, out var allocation);
        return new AllocatedBuffer(buffer, allocation);
    }

    public CommandBuffer BeginSingleTimeCommands(CommandPool pool)
    {
        var cmd = AllocateCommandBuffers(1, pool)[0];
        cmd.Begin();
        return cmd;
    }
    public void EndSingleTimeCommands(CommandBuffer cmd, CommandPool pool, Queue queue)
    {
        cmd.End();
        var commandBuffer = (Silk.NET.Vulkan.CommandBuffer)cmd;
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };
        queue.QueueSubmit(submitInfo, default);
        queue.WaitForQueue();
        FreeCommandBuffer(cmd, pool);
    }
}