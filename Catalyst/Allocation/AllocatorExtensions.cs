using Silk.NET.Vulkan;

namespace Catalyst.Allocation;

public static class AllocatorExtensions
{
    public static void AllocateImage(this IAllocator allocator, Image image, AllocationCreateInfo allocInfo, out Allocation allocation)
    {
        vk.GetImageMemoryRequirements(allocator.Device.VkDevice, image, out var memReq);
        allocInfo.Size = memReq.Size;
        allocInfo.MemoryTypeIndex = allocator.FindMemoryType(memReq.MemoryTypeBits, allocInfo.Usage);
        allocator.Allocate(allocInfo, out allocation);
        vk.BindImageMemory(allocator.Device.VkDevice, image, allocation.AllocatedMemory, 0);
    }
    
    public static void AllocateBuffer(this IAllocator allocator, Silk.NET.Vulkan.Buffer buffer, AllocationCreateInfo allocInfo, out Allocation allocation)
    { 
        vk.GetBufferMemoryRequirements(allocator.Device.VkDevice, buffer, out var memReq);
        allocInfo.Size = memReq.Size;
        allocInfo.MemoryTypeIndex = allocator.FindMemoryType(memReq.MemoryTypeBits, allocInfo.Usage);
        allocator.Allocate(allocInfo, out allocation);
        vk.BindBufferMemory(allocator.Device.VkDevice, buffer, allocation.AllocatedMemory, 0);
    }
}