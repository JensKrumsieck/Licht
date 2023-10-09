namespace Licht.Vulkan.Memory;

public struct AllocatedBuffer
{
    public readonly Buffer Buffer;
    public readonly Allocation Allocation;
    
    internal AllocatedBuffer(Buffer buffer, Allocation allocation)
    {
        Buffer = buffer;
        Allocation = allocation;
    }
}