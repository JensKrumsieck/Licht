using Silk.NET.Vulkan;

namespace Catalyst.Tools;

public static class SelectorTools
{
    public static T SelectByScore<T>(T[] array, Func<T, int>? selector) =>
        array.MaxBy(i => selector?.Invoke(i)) ?? array[0];

    public static int DefaultGpuSelector(PhysicalDevice d)
    {
        var score = 0;
        vk.GetPhysicalDeviceProperties(d, out var props);
        score += props.DeviceType switch
        {
            PhysicalDeviceType.DiscreteGpu => 100,
            PhysicalDeviceType.IntegratedGpu => 70,
            PhysicalDeviceType.Cpu => 50,
            _ => 0
        };
        score += (int) props.Limits.MaxViewports;
        score += (int) props.Limits.MaxFramebufferWidth;
        return score;
    }
}