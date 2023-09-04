using Catalyze.Tools;
using Silk.NET.Vulkan;

namespace Catalyze;

public unsafe struct Buffer : IDisposable, IConvertibleTo<Silk.NET.Vulkan.Buffer>
{
    private readonly Device _device;
    private readonly uint _size;
    public ulong BufferSize => _size;

    public Silk.NET.Vulkan.Buffer VkBuffer;
    public DeviceMemory Memory => _allocation.AllocatedMemory;
    
    public ulong Handle => VkBuffer.Handle;
    private Allocation.Allocation _allocation;
    public void* PMappedData => _allocation.PMappedData;

    internal Buffer(Device device, ulong size, Silk.NET.Vulkan.Buffer buffer, Allocation.Allocation allocation)
    {
        _device = device;
        _size = (uint) size;
        VkBuffer = buffer;
        _allocation = allocation;
    }
    
    public static ulong GetAlignment(ulong bufferSize, ulong minOffsetAlignment)
    {
        if (minOffsetAlignment > 0) return ((bufferSize - 1) / minOffsetAlignment + 1) * minOffsetAlignment;
        return bufferSize;
    }

    public Result Map(ulong size = Vk.WholeSize, ulong offset = 0) => _allocation.Map(size, offset);
    public void Unmap() => _allocation.Unmap();

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
            Memory = _allocation.AllocatedMemory,
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
            Memory = _allocation.AllocatedMemory,
            Offset = offset,
            Size = size
        };
        return vk.InvalidateMappedMemoryRanges(_device, 1, &mappedRange);
    }

    public DescriptorBufferInfo DescriptorInfo(ulong size = Vk.WholeSize, ulong offset = 0) => new(VkBuffer, offset, size);

    public static implicit operator Silk.NET.Vulkan.Buffer(Buffer b) => b.VkBuffer;

    public Silk.NET.Vulkan.Buffer Convert() => VkBuffer;
    public void Dispose()
    {
        Unmap();
        vk.DestroyBuffer(_device, VkBuffer, null);
        _allocation.Dispose();
    }
}