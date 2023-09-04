using Silk.NET.Vulkan;

namespace Catalyze.Allocation;

public interface IAllocator : IDisposable
{
    public GraphicsDevice Device { get; }
    public void Bind(GraphicsDevice device); 

    void Allocate(AllocationCreateInfo createInfo, out Allocation alloc);
    void Free(Allocation alloc);
    
    ulong AllocatedSize(uint memoryType);
    uint NumberOfAllocations();

    public uint FindMemoryType(uint filter, MemoryPropertyFlags flags)
    {
        vk.GetPhysicalDeviceMemoryProperties(Device.VkPhysicalDevice, out var properties);
        for (var i = 0; i < properties.MemoryTypeCount; i++)
        {
            if ((filter & (uint)(1 << i)) != 0u && (properties.MemoryTypes[i].PropertyFlags & flags) == flags)
                return (uint)i;
        }
        throw new Exception("unable to find suitable memory type");
    }
}