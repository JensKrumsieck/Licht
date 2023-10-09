using System.Runtime.CompilerServices;
using Licht.Vulkan.Extensions;
using Licht.Vulkan.Memory;
using Silk.NET.Vulkan;

namespace Licht.Vulkan;

public unsafe class VkBuffer : IDisposable
{
    private readonly VkGraphicsDevice _device;
    private readonly ulong _size;
    public ulong BufferSize => _size;

    public AllocatedBuffer AllocatedBuffer;
    public Buffer Buffer => AllocatedBuffer.Buffer;
    public DeviceMemory Memory => AllocatedBuffer.Allocation.AllocatedMemory;

    public VkBuffer(VkGraphicsDevice device, ulong size, BufferUsageFlags usageFlags, MemoryPropertyFlags memoryFlags)
    {
        _device = device;
        _size = size;
        vk.GetPhysicalDeviceProperties(_device.PhysicalDevice, out var props);
        if (usageFlags == BufferUsageFlags.UniformBufferBit) _size = GetAlignment(_size, props.Limits.MinUniformBufferOffsetAlignment);
        else _size = GetAlignment(_size, 256);
        AllocatedBuffer = _device.CreateBuffer(_size, usageFlags, memoryFlags);
    }
    
    public static ulong GetAlignment(ulong bufferSize, ulong minOffsetAlignment)
    {
        if (minOffsetAlignment > 0) return ((bufferSize - 1) / minOffsetAlignment + 1) * minOffsetAlignment;
        return bufferSize;
    }

    public Result Map(ref void* pMappedData, ulong size = Vk.WholeSize, ulong offset = 0)
    {
        return AllocatedBuffer.Allocation.Map(ref pMappedData, size, offset);
    }
    public void Unmap() => AllocatedBuffer.Allocation.Unmap();

    public void WriteToBuffer(void* data, void* destination, ulong size = Vk.WholeSize, ulong offset = 0)
    {
        if(size == Vk.WholeSize) System.Buffer.MemoryCopy(data, destination, _size, _size);
        else
        {
            if(size > _size) return;
            var memoryOffset = (ulong*) ((ulong)destination + offset);
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
        return vk.FlushMappedMemoryRanges(_device.Device, 1, &mappedRange);   
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
        return vk.InvalidateMappedMemoryRanges(_device.Device, 1, &mappedRange);
    }

    public void CopyToImage(VkImage vkImage)
    {
        var cmd = _device.BeginSingleTimeCommands();
        var layers = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1);
        var copyRegion = new BufferImageCopy(0, 0, 0, layers, default, vkImage.ImageExtent);
        cmd.CopyBufferToImage(this, vkImage, ImageLayout.TransferDstOptimal, copyRegion);
        _device.EndSingleTimeCommands(cmd);
    }

    public void CopyToBuffer(VkBuffer vkBuffer)
    {
        var cmd = _device.BeginSingleTimeCommands();
        var copyRegion = new BufferCopy {Size = BufferSize, SrcOffset = 0, DstOffset = 0};
        cmd.CopyBuffer(this, vkBuffer, copyRegion);
        _device.EndSingleTimeCommands(cmd);
    }

    public static VkBuffer CreateAndCopyToStagingBuffer(VkGraphicsDevice device, void* data, ulong size)
    {
        var stagingBuffer = new VkBuffer(device, size, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit);
        var destination = IntPtr.Zero.ToPointer();
        stagingBuffer.Map(ref destination).Validate();
        stagingBuffer.WriteToBuffer(data, destination, size);
        stagingBuffer.Flush().Validate();
        stagingBuffer.Unmap();
        return stagingBuffer;
    }
    
    public static VkBuffer CreateAndCopyToStagingBuffer<T>(VkGraphicsDevice device, T[] data) where T : unmanaged
    {
        var count = data.Length;
        var bufferSize = (ulong) (Unsafe.SizeOf<T>() * count);
        fixed (void* pData = data)
            return CreateAndCopyToStagingBuffer(device, pData, bufferSize);
    }
    
    public DescriptorBufferInfo DescriptorInfo(ulong size = Vk.WholeSize, ulong offset = 0) => new(AllocatedBuffer.Buffer, offset, size);

    public static implicit operator Buffer(VkBuffer b) => b.AllocatedBuffer.Buffer;

    public void Dispose()
    {
        Unmap();
        vk.DestroyBuffer(_device.Device, AllocatedBuffer.Buffer, null);
        AllocatedBuffer.Allocation.Dispose();
        GC.SuppressFinalize(this);
    }
}
