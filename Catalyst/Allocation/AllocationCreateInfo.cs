using Silk.NET.Vulkan;

namespace Catalyst.Allocation;

public struct AllocationCreateInfo
{
    public MemoryPropertyFlags Usage;
    public uint MemoryTypeIndex;
    public ulong Size;
}