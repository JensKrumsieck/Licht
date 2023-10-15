using Silk.NET.Vulkan;

namespace Licht.Vulkan.Memory;

public struct AllocationCreateInfo
{
    public MemoryPropertyFlags Usage;
    public uint MemoryTypeIndex;
    public ulong Size;
}