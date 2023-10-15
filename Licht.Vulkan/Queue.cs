using Silk.NET.Vulkan;

namespace Licht.Vulkan;

public readonly unsafe struct Queue
{
    private readonly Silk.NET.Vulkan.Queue _queue;
    
    private readonly ulong Handle => (ulong)_queue.Handle;

    public static implicit operator Silk.NET.Vulkan.Queue(Queue q) => q._queue;
    public static implicit operator Silk.NET.Vulkan.Queue*(Queue q) => &q._queue;
    public static implicit operator Queue(Silk.NET.Vulkan.Queue q) => new(q);
    
    public Queue(Silk.NET.Vulkan.Queue vkQueue) => _queue = vkQueue;

    public Result WaitForQueue() => vk.QueueWaitIdle(_queue);
    public Result QueueSubmit(SubmitInfo submitInfo, Fence fence) => vk.QueueSubmit(_queue, 1, submitInfo, fence);
}
