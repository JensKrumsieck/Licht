using Silk.NET.Vulkan;

namespace Catalyze.Allocation;

public struct AllocationCreateInfo
{
    public MemoryPropertyFlags Usage;
    public uint MemoryTypeIndex;
    public ulong Size;
}