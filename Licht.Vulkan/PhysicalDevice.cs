using Silk.NET.Vulkan;

namespace Licht.Vulkan;

public readonly unsafe partial struct PhysicalDevice
{
    public static implicit operator PhysicalDevice(Silk.NET.Vulkan.PhysicalDevice p) => new(p);

    public PhysicalDevice(Silk.NET.Vulkan.PhysicalDevice gpu)
    {
        _physicalDevice = gpu;
    }

    public readonly PhysicalDeviceProperties GetProperties() => vk.GetPhysicalDeviceProperties(this);
    public readonly PhysicalDeviceProperties2 GetProperties2() => vk.GetPhysicalDeviceProperties2(this);
    public readonly PhysicalDeviceMemoryProperties GetMemoryProperties() => vk.GetPhysicalDeviceMemoryProperties(this);
    public readonly QueueFamilyProperties[] GetQueueFamilyProperties()
    {
        var count = 0u;
        vk.GetPhysicalDeviceQueueFamilyProperties(this, &count, null);
        var properties = new QueueFamilyProperties[count];
        fixed(QueueFamilyProperties* pProperties = properties)
            vk.GetPhysicalDeviceQueueFamilyProperties(this, &count, pProperties);
        return properties;
    }
    public Format FindFormat(Format[] candidates, ImageTiling tiling, FormatFeatureFlags formatFeatureFlags)
    {
        foreach (var candidate in candidates)
        {
            vk.GetPhysicalDeviceFormatProperties(_physicalDevice, candidate, out var props);
            if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & formatFeatureFlags) == formatFeatureFlags)
                return candidate;
            if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & formatFeatureFlags) == formatFeatureFlags)
                return candidate;
        }
        throw new Exception($"Unable to find supported format for {tiling} and {formatFeatureFlags}");
    }
}