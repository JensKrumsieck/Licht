using System.Diagnostics.Contracts;
using Silk.NET.Vulkan;

namespace Licht.Vulkan.Memory;

public unsafe class Allocation : IDisposable
{
    private readonly IAllocator _allocator;
    public DeviceMemory AllocatedMemory;
    public readonly uint Type;
    public readonly uint Id;
    public readonly ulong Size;
    public readonly uint Offset;
    private bool _hostMapped;

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
    public Result Map(ref void* pMappedData, ulong size = Vk.WholeSize, ulong offset = 0)
    { 
        _hostMapped = true;
        return vk.MapMemory(_allocator.Device.Device, AllocatedMemory, offset, size, 0, ref pMappedData);
    }

    [Pure]
    public void Unmap()
    {
        _hostMapped = false;
        vk.UnmapMemory(_allocator.Device.Device, AllocatedMemory);
    }
    
    public void Dispose()
    {
        if(_hostMapped) Unmap();
        _allocator.Free(this);
    }
}