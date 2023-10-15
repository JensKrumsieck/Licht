using Silk.NET.Vulkan;

namespace Licht.Vulkan.Memory;

public static class AllocatorExtensions
{
    public static void AllocateImage(this IAllocator allocator, Image image, AllocationCreateInfo allocInfo, out Allocation allocation)
    {
        vk.GetImageMemoryRequirements(allocator.Device.Device, image, out var memReq);
        allocInfo.Size = memReq.Size;
        allocInfo.MemoryTypeIndex = allocator.FindMemoryType(memReq.MemoryTypeBits, allocInfo.Usage);
        allocator.Allocate(allocInfo, out allocation);
        vk.BindImageMemory(allocator.Device.Device, image, allocation.AllocatedMemory, 0);
    }
    
    public static void AllocateBuffer(this IAllocator allocator, Silk.NET.Vulkan.Buffer buffer, AllocationCreateInfo allocInfo, out Allocation allocation)
    { 
        vk.GetBufferMemoryRequirements(allocator.Device.Device, buffer, out var memReq);
        allocInfo.Size = memReq.Size;
        allocInfo.MemoryTypeIndex = allocator.FindMemoryType(memReq.MemoryTypeBits, allocInfo.Usage);
        allocator.Allocate(allocInfo, out allocation);
        vk.BindBufferMemory(allocator.Device.Device, buffer, allocation.AllocatedMemory, 0);
    }
}