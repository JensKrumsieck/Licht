using System.Runtime.InteropServices;
using Licht.Vulkan.Extensions;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;

namespace Licht.Vulkan.Memory;

public sealed unsafe class PassthroughAllocator : IAllocator
{
    private struct AllocatorState
    {
        public ulong* MemoryTypeAllocSizes;
        public uint TotalAllocations;
        public ulong TotalSize;
        public VkGraphicsDevice Context;
    }

    private AllocatorState _state;
    public VkGraphicsDevice Device => _state.Context;

    private readonly ILogger? _logger;

    public PassthroughAllocator(ILogger? logger = null!) => _logger = logger;
    
    public void Bind(VkGraphicsDevice device)
    {
        _state.Context = device;
        var memoryProperties = device.PhysicalDevice.GetPhysicalDeviceMemoryProperties();
        _state.MemoryTypeAllocSizes = (ulong*) Marshal.AllocHGlobal((nint) (sizeof(uint) * memoryProperties.MemoryTypeCount));
    }

    private void Deactivate() => Marshal.FreeHGlobal((nint) _state.MemoryTypeAllocSizes);

    public void Allocate(AllocationCreateInfo createInfo, out Allocation alloc)
    {
        _state.TotalAllocations++;
        _state.MemoryTypeAllocSizes[createInfo.MemoryTypeIndex] += createInfo.Size;
        _state.TotalSize += createInfo.Size;
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = createInfo.Size,
            MemoryTypeIndex = createInfo.MemoryTypeIndex
        };
        vk.AllocateMemory(_state.Context.Device, allocInfo, null, out var allocatedMemory).Validate(_logger);
        alloc = new Allocation(this, allocatedMemory, createInfo.MemoryTypeIndex, 0, createInfo.Size, 0);
        _logger?.LogDebug("Allocated {Size} kB of Memory - Total Allocations {Count} - Total allocated Memory {TotalSize} MB", 
            alloc.Size / 1024,
            _state.TotalAllocations, 
            _state.TotalSize / 1024 / 1024);
    }

    public void Free(Allocation alloc)
    {
        _state.TotalAllocations--;
        _state.MemoryTypeAllocSizes[alloc.Type] -= alloc.Size;
        _state.TotalSize -= alloc.Size;
        vk.FreeMemory(_state.Context.Device, alloc.AllocatedMemory, null);
        _logger?.LogDebug("Freed {Size} kB of Memory - Total Allocations {Count} - Total allocated Memory {TotalSize} MB",
            alloc.Size / 1024, 
            _state.TotalAllocations, 
            _state.TotalSize / 1024 / 1024);
    }

    public ulong AllocatedSize(uint memoryType) => _state.MemoryTypeAllocSizes[memoryType];

    public uint NumberOfAllocations() => _state.TotalAllocations;

    public void Dispose() => Deactivate();
}