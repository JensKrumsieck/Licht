using Licht.Vulkan.Memory;
using Silk.NET.Vulkan;

namespace Licht.Vulkan;

public unsafe class VkBuffer : IDisposable
{
    private readonly Device _device;
    private readonly uint _size;
    public ulong BufferSize => _size;

    public AllocatedBuffer AllocatedBuffer;
    public DeviceMemory Memory => AllocatedBuffer.Allocation.AllocatedMemory;
    public void* PMappedData => AllocatedBuffer.Allocation.PMappedData;

    internal VkBuffer(Device device, ulong size, AllocatedBuffer allocatedBuffer)
    {
        _device = device;
        _size = (uint) size;
        AllocatedBuffer = allocatedBuffer;
    }
    
    public static ulong GetAlignment(ulong bufferSize, ulong minOffsetAlignment)
    {
        if (minOffsetAlignment > 0) return ((bufferSize - 1) / minOffsetAlignment + 1) * minOffsetAlignment;
        return bufferSize;
    }

    public Result Map(ulong size = Vk.WholeSize, ulong offset = 0) => AllocatedBuffer.Allocation.Map(size, offset);
    public void Unmap() => AllocatedBuffer.Allocation.Unmap();

    public void WriteToBuffer(void* data, ulong size = Vk.WholeSize, ulong offset = 0)
    {
        if(size == Vk.WholeSize) System.Buffer.MemoryCopy(data, PMappedData, _size, _size);
        else
        {
            if(size > _size) return;
            var memoryOffset = (ulong*) ((ulong)PMappedData + offset);
            System.Buffer.MemoryCopy(data, memoryOffset, size, size);
        }
    }

    public Result Flush(ulong size = Vk.WholeSize, ulong offset = 0)
    {
        var mappedRange = new MappedMemoryRange
        {
            SType = StructureType.MappedMemoryRange,
            Memory = AllocatedBuffer.Allocation.AllocatedMemory,
            Offset = offset,
            Size = size
        };
        return vk.FlushMappedMemoryRanges(_device, 1, &mappedRange);   
    }
    
    public Result Invalidate(ulong size = Vk.WholeSize, ulong offset = 0)
    {
        var mappedRange = new MappedMemoryRange
        {
            SType = StructureType.MappedMemoryRange,
            Memory = AllocatedBuffer.Allocation.AllocatedMemory,
            Offset = offset,
            Size = size
        };
        return vk.InvalidateMappedMemoryRanges(_device, 1, &mappedRange);
    }

    public DescriptorBufferInfo DescriptorInfo(ulong size = Vk.WholeSize, ulong offset = 0) => new(AllocatedBuffer.Buffer, offset, size);

    public static implicit operator Buffer(VkBuffer b) => b.AllocatedBuffer.Buffer;

    public void Dispose()
    {
        Unmap();
        vk.DestroyBuffer(_device, AllocatedBuffer.Buffer, null);
        AllocatedBuffer.Allocation.Dispose();
        GC.SuppressFinalize(this);
    }
}
