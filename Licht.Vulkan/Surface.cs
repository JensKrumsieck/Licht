using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Licht.Vulkan;

public readonly unsafe struct Surface : IDisposable
{
    private readonly Instance _instance;
    private readonly KhrSurface _khrSurface;
    private readonly Silk.NET.Vulkan.SurfaceKHR _surface;
    private readonly ulong Handle => _surface.Handle;

    public static implicit operator Silk.NET.Vulkan.SurfaceKHR(Surface s) => s._surface;

    public Surface(Instance instance, IVkSurfaceSource surfaceSource)
    {
        _instance = instance;
        vk.TryGetInstanceExtension(_instance, out _khrSurface);
        _surface = surfaceSource.VkSurface!
            .Create<AllocationCallbacks>(new VkHandle((nint)_instance.Handle), null)
            .ToSurface();
    }

    public void Dispose()
    {
        _khrSurface.DestroySurface(_instance, _surface, null);
        _khrSurface.Dispose();
    }
}