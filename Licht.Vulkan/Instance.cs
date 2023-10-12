using Silk.NET.Core.Contexts;
using Silk.NET.Vulkan;

namespace Licht.Vulkan;
public readonly partial struct Instance
{
    public PhysicalDevice SelectPhysicalDevice()
    {
        var devices = vk.GetPhysicalDevices(_instance);
        var gpu = devices.FirstOrDefault(gpu =>
        {
            vk.GetPhysicalDeviceProperties(gpu, out var p);
            return p.DeviceType == PhysicalDeviceType.DiscreteGpu;
        });
        if (gpu.Handle == 0) gpu = devices.First();
        return gpu;
    }

    public Surface CreateSurface(IVkSurfaceSource surfaceSource) => new Surface(this, surfaceSource);
}
