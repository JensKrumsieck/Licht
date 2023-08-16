﻿using Silk.NET.Vulkan;

namespace Catalyst.Allocation;

public unsafe struct Allocation : IDisposable
{
    private readonly IAllocator _allocator;
    public DeviceMemory AllocatedMemory;
    public uint Type;
    public uint Id;
    public ulong Size;
    public uint Offset;

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

    public Result Map(ulong size = Vk.WholeSize, ulong offset = 0) =>
        _allocator.MapMemory(AllocatedMemory, offset, size, ref PMappedData);

    public void Unmap()
    {
        if(PMappedData is null) return;
        _allocator.UnmapMemory(AllocatedMemory);
        PMappedData = null;
    }

    public void Dispose()
    {
        if(PMappedData is null) Unmap();
        _allocator.Free(this);
    }
}