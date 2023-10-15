using Licht.Vulkan.Memory;
using Silk.NET.Vulkan;

namespace Licht.Vulkan;

public unsafe partial struct Device
{
    public Queue GetQueue(uint familyIndex, uint queueIndex = 0)
    {
        vk.GetDeviceQueue(this, familyIndex, queueIndex, out var queue);
        return queue;
    }

    public void SubmitCommandBufferToQueue(Queue q, CommandBuffer cmd, Fence f)
    {
        var vkCmd = (Silk.NET.Vulkan.CommandBuffer) cmd;
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &vkCmd
        };
        q.QueueSubmit(submitInfo, f);
    }

    public void WaitIdle() => vk.DeviceWaitIdle(_device);
    public void WaitForFence(Fence fence) => vk.WaitForFences(_device, 1u, fence, true, ulong.MaxValue);
    public Result ResetFence(Fence fence) => vk.ResetFences(_device, 1, fence);
    
    public CommandBuffer[] AllocateCommandBuffers(CommandPool pool, uint count = 1)
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

    public void FreeCommandBuffer(CommandBuffer commandBuffer, CommandPool pool)
    {
        var vkCmd = (Silk.NET.Vulkan.CommandBuffer) commandBuffer;
        vk.FreeCommandBuffers(_device, pool, 1, &vkCmd);
    }

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
        var cmd = AllocateCommandBuffers(pool)[0];
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
    
    public DescriptorSetLayout CreateDescriptorSetLayout(DescriptorSetLayoutBinding[] bindings)
    {
        fixed (DescriptorSetLayoutBinding* pBindings = bindings)
        {
            var layoutCreateInfo = new DescriptorSetLayoutCreateInfo
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)bindings.Length,
                PBindings = pBindings
            };
            return new DescriptorSetLayout(this, layoutCreateInfo);
        }
    }
    public DescriptorSetLayout CreateDescriptorSetLayout(DescriptorSetLayoutBinding binding)
    {
        var layoutCreateInfo = new DescriptorSetLayoutCreateInfo
        {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = 1,
                PBindings = &binding
        };
        return new DescriptorSetLayout(this, layoutCreateInfo);
    }
    
    public DescriptorPool CreateDescriptorPool(DescriptorPoolSize[] poolSizes)
    {
        fixed (DescriptorPoolSize* pPoolSizes = poolSizes)
        {
            var createInfo = new DescriptorPoolCreateInfo
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint) poolSizes.Length,
                PPoolSizes = pPoolSizes,
                MaxSets = 1,
                Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit
            };
            vk.CreateDescriptorPool(_device, createInfo, null, out var descriptorPool);
            return new DescriptorPool(this, descriptorPool);
        }
    }
    public void UpdateDescriptorSetImage(ref DescriptorSet set, DescriptorImageInfo imageInfo, DescriptorType type,
        uint binding = 0)
    {
        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = binding,
            DstArrayElement = 0,
            DescriptorCount = 1,
            PImageInfo = &imageInfo,
            DescriptorType = type
        };
        vk.UpdateDescriptorSets(_device, 1, &write, 0, default);
    }
    
}
