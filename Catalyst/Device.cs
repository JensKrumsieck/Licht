using Catalyst.Allocation;
using Catalyst.Tools;
using Silk.NET.Vulkan;

namespace Catalyst;

public readonly unsafe struct Device : IDisposable, IConvertibleTo<Silk.NET.Vulkan.Device>
{
    public readonly uint MainQueueIndex = 0;
    public readonly Silk.NET.Vulkan.Device VkDevice;
    public readonly PhysicalDevice PhysicalDevice;
    public IntPtr Handle => VkDevice.Handle;
    public readonly Queue MainQueue;

    public Device(PhysicalDevice gpu) : this(gpu, new DeviceInfo()){}
    
    public Device(PhysicalDevice gpu, DeviceInfo deviceInfo)
    {
        deviceInfo.Validate();
        PhysicalDevice = gpu;
        var defaultPriority = 1.0f;
        var queueFamilies = gpu.Enumerate<QueueFamilyProperties>(vk.GetPhysicalDeviceQueueFamilyProperties);
        for (var i = 0u; i < queueFamilies.Length; i++)
        {
            if (queueFamilies[i].QueueFlags.HasFlag(QueueFlags.GraphicsBit) ||
                queueFamilies[i].QueueFlags.HasFlag(QueueFlags.ComputeBit))
            {
                MainQueueIndex = i;
                break;
            }
        }

        var queueCreateInfo = new DeviceQueueCreateInfo
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueCount = 1,
            QueueFamilyIndex = MainQueueIndex,
            PQueuePriorities = &defaultPriority
        };
        using var extensions = new ByteStringList(deviceInfo.EnabledExtensions);
        using var layers = new ByteStringList(deviceInfo.EnabledLayers);
        var createInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &queueCreateInfo,
            PEnabledFeatures = &deviceInfo.DesiredFeatures,
            EnabledExtensionCount = extensions.Count,
            PpEnabledExtensionNames = extensions,
            EnabledLayerCount = layers.Count,
            PpEnabledLayerNames = layers
        };
        vk.CreateDevice(gpu, createInfo, null, out VkDevice).Validate();
        vk.GetDeviceQueue(VkDevice, MainQueueIndex, 0, out MainQueue);
    }

    public void WaitIdle() => vk.DeviceWaitIdle(VkDevice);
    public Result WaitForFence(Fence fence) => vk.WaitForFences(VkDevice, 1, fence, true, ulong.MaxValue);
    public Result WaitForQueue() => vk.QueueWaitIdle(MainQueue);
    public Result ResetFence(Fence fence) => vk.ResetFences(VkDevice, 1, fence);

    public Result SubmitMainQueue(SubmitInfo submitInfo, Fence fence) => vk.QueueSubmit(MainQueue, 1, submitInfo, fence);

    public CommandBuffer[] AllocateCommandBuffers(uint count, CommandPool commandPool)
    {
        var commandBuffers = new CommandBuffer[count];
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = commandPool,
            CommandBufferCount = count
        };
        fixed (void* pCommandBuffers = commandBuffers)
            vk.AllocateCommandBuffers(VkDevice, allocInfo, (Silk.NET.Vulkan.CommandBuffer*)pCommandBuffers).Validate();
        return commandBuffers;
    }

    public void FreeCommandBuffers(CommandBuffer[] commandBuffers, CommandPool commandPool) =>
        vk.FreeCommandBuffers(VkDevice, commandPool, (uint) commandBuffers.Length,
                              commandBuffers.AsArray<CommandBuffer, Silk.NET.Vulkan.CommandBuffer>());
    public void FreeCommandBuffer(CommandBuffer commandBuffer, CommandPool commandPool) =>
        vk.FreeCommandBuffers(VkDevice, commandPool, 1, commandBuffer);

    public Format FindFormat(Format[] candidates, ImageTiling tiling, FormatFeatureFlags formatFeatureFlags)
    {
        foreach (var candidate in candidates)
        {
            vk.GetPhysicalDeviceFormatProperties(PhysicalDevice, candidate, out var props);
            if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & formatFeatureFlags) == formatFeatureFlags)
                return candidate;
            if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & formatFeatureFlags) == formatFeatureFlags)
                return candidate;
        }

        throw new Exception($"Unable to find supported format for {tiling} and {formatFeatureFlags}");
    }

    public AllocatedImage CreateImage(IAllocator allocator, ImageCreateInfo info, MemoryPropertyFlags propertyFlags)
    {
        vk.CreateImage(VkDevice, info, null, out var image).Validate();
        var allocInfo = new AllocationCreateInfo {Usage = propertyFlags};
        allocator.AllocateImage(image, allocInfo, out var allocation);
        return new AllocatedImage(image, allocation);
    }
    public void DestroyImage(AllocatedImage image)
    {
        vk.DestroyImage(VkDevice, image.Image, null);
        image.Allocation.Dispose();
    }

    public ImageView CreateImageView(ImageViewCreateInfo info)
    {
        vk.CreateImageView(VkDevice, info, null, out var imageView).Validate();
        return imageView;
    }
    public void DestroyImageView(ImageView view) => vk.DestroyImageView(VkDevice, view, null);
    
    public Sampler CreateSampler(SamplerCreateInfo createInfo)
    {
        vk.CreateSampler(VkDevice, createInfo, default, out var sampler).Validate();
        return sampler;
    }

    public void DestroySampler(Sampler sampler) => vk.DestroySampler(VkDevice, sampler, null);

    public Buffer CreateBuffer(IAllocator allocator, uint instanceSize, uint instanceCount, BufferUsageFlags usageFlags, MemoryPropertyFlags memoryFlags)
    {
        ulong alignedSize = instanceSize * instanceCount;
        vk.GetPhysicalDeviceProperties(PhysicalDevice, out var props);
        if (usageFlags == BufferUsageFlags.UniformBufferBit) alignedSize = Buffer.GetAlignment(alignedSize, props.Limits.MinUniformBufferOffsetAlignment);
        else alignedSize = Buffer.GetAlignment(alignedSize, 256);
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = alignedSize,
            Usage = usageFlags,
            SharingMode = SharingMode.Exclusive
        };
        vk.CreateBuffer(VkDevice, bufferInfo, null, out var buffer).Validate();
        var allocInfo = new AllocationCreateInfo {Usage = memoryFlags};
        allocator.AllocateBuffer(buffer, allocInfo, out var allocation);
        return new Buffer(this, alignedSize, buffer, allocation);
    }
    
    public static implicit operator Silk.NET.Vulkan.Device(Device d) => d.VkDevice;

    public void Dispose() => vk.DestroyDevice(VkDevice, null);
    public Silk.NET.Vulkan.Device Convert() => VkDevice;
}