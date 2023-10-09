using System.Diagnostics.Contracts;
using Silk.NET.Vulkan;

namespace Licht.Vulkan.Memory;

public unsafe struct Allocation : IDisposable
{
    private readonly IAllocator _allocator;
    public DeviceMemory AllocatedMemory;
    public readonly uint Type;
    public readonly uint Id;
    public readonly ulong Size;
    public readonly uint Offset;

    public void* PMappedData;

    public Allocation(IAllocator allocator, DeviceMemory allocatedMemory, uint type, uint id, ulong size, uint offset)
    {
        _allocator = allocator;
        AllocatedMemory = allocatedMemory;
        Type = type;
        Id = id;
        Size = size;
        Offset = offset;
    }

    [Pure]
    public Result Map(ulong size = Vk.WholeSize, ulong offset = 0) =>
        vk.MapMemory(_allocator.Device.Device, AllocatedMemory, offset, size, 0, ref PMappedData);
    
    [Pure]
    public void Unmap()
    {
        if(PMappedData is null) return;
        vk.UnmapMemory(_allocator.Device.Device, AllocatedMemory);
        PMappedData = null;
    }
    
    [Pure]
    public void Dispose()
    {
        if(PMappedData is null) Unmap();
        _allocator.Free(this);
    }
}