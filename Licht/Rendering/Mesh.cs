using System.Diagnostics;
using System.Runtime.CompilerServices;
using Licht.Vulkan;
using Silk.NET.Vulkan;
using CommandBuffer = Licht.Vulkan.CommandBuffer;

namespace Licht.Rendering;

public unsafe class Mesh : IDisposable
{
    private VkBuffer _vertexBuffer;
    private uint _vertexCount;
    private VkBuffer? _indexBuffer;
    private uint _indexCount;

    public Mesh(VkGraphicsDevice device, Vertex[] vertices, uint[]? indices = null)
    {
        _vertexBuffer = CreateVertexBuffer(ref vertices, device);
        _indexBuffer = CreateIndexBuffer(ref indices, device);
    }
    
    public void Bind(CommandBuffer commandBuffer)
    {
        commandBuffer.BindVertexBuffers(0, 1, _vertexBuffer.Buffer, 0);
        if (_indexBuffer is not null)
            commandBuffer.BindIndexBuffer(_indexBuffer.Buffer, 0, IndexType.Uint32);
    }
    
    public void Draw(CommandBuffer commandBuffer)
    {
        if (_indexBuffer is not null) commandBuffer.DrawIndexed(_indexCount, 1, 0, 0, 0);
        else commandBuffer.Draw(_vertexCount, 1, 0, 0);
    }

    private VkBuffer CreateVertexBuffer(ref Vertex[] vertices, VkGraphicsDevice device)
    {
        _vertexCount = (uint) vertices.Length;
        Debug.Assert(_vertexCount >= 3);

        var bufferSize = (uint)Unsafe.SizeOf<Vertex>() * _vertexCount;

        using var stagingBuffer = VkBuffer.CreateAndCopyToStagingBuffer(device, vertices);
        
        _vertexBuffer = new VkBuffer(
            device,
            bufferSize,
            BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.DeviceLocalBit);

        stagingBuffer.CopyToBufferImmediate(_vertexBuffer);
        return _vertexBuffer;
    }

    private VkBuffer? CreateIndexBuffer(ref uint[]? indices, VkGraphicsDevice device)
    {
        if(indices is null || indices.Length == 0) return null;
        
        _indexCount = (uint) indices!.Length;
        var bufferSize = sizeof(uint) * _indexCount;
        using var stagingBuffer = VkBuffer.CreateAndCopyToStagingBuffer(device, indices);

        _indexBuffer = new VkBuffer(
            device,
            bufferSize,
            BufferUsageFlags.IndexBufferBit | BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.DeviceLocalBit);
        
        stagingBuffer.CopyToBufferImmediate(_indexBuffer);
        return _indexBuffer;
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _indexBuffer?.Dispose();
    }
}
