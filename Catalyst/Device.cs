using Catalyst.Allocation;
using Catalyst.Tools;
using Silk.NET.Vulkan;

namespace Catalyst;

public readonly unsafe struct Device : IDisposable
{
    public readonly uint MainQueueIndex = 0;
    public readonly Silk.NET.Vulkan.Device VkDevice;
    public readonly PhysicalDevice PhysicalDevice;
    public IntPtr Handle => VkDevice.Handle;
    public readonly Queue MainQueue;

    public Device(PhysicalDevice gpu) : this(gpu, new DeviceInfo()){}
    
    public Device(PhysicalDevice gpu, DeviceInfo deviceInfo)
    {
        deviceInfo.Validate();
        PhysicalDevice = gpu;
        var defaultPriority = 1.0f;
        var queueFamilies = gpu.Enumerate<QueueFamilyProperties>(vk.GetPhysicalDeviceQueueFamilyProperties);
        for (var i = 0u; i < queueFamilies.Length; i++)
        {
            if (queueFamilies[i].QueueFlags.HasFlag(QueueFlags.GraphicsBit) ||
                queueFamilies[i].QueueFlags.HasFlag(QueueFlags.ComputeBit))
            {
                MainQueueIndex = i;
                break;
            }
        }

        var queueCreateInfo = new DeviceQueueCreateInfo
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueCount = 1,
            QueueFamilyIndex = MainQueueIndex,
            PQueuePriorities = &defaultPriority
        };
        using var extensions = new ByteStringList(deviceInfo.EnabledExtensions);
        using var layers = new ByteStringList(deviceInfo.EnabledLayers);
        var createInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &queueCreateInfo,
            PEnabledFeatures = &deviceInfo.DesiredFeatures,
            EnabledExtensionCount = extensions.Count,
            PpEnabledExtensionNames = extensions,
            EnabledLayerCount = layers.Count,
            PpEnabledLayerNames = layers
        };
        vk.CreateDevice(gpu, createInfo, null, out VkDevice).Validate();
        vk.GetDeviceQueue(VkDevice, MainQueueIndex, 0, out MainQueue);
    }

    public void WaitIdle() => vk.DeviceWaitIdle(VkDevice);

    public Format FindFormat(Format[] candidates, ImageTiling tiling, FormatFeatureFlags formatFeatureFlags)
    {
        foreach (var candidate in candidates)
        {
            vk.GetPhysicalDeviceFormatProperties(PhysicalDevice, candidate, out var props);
            if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & formatFeatureFlags) == formatFeatureFlags)
                return candidate;
            if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & formatFeatureFlags) == formatFeatureFlags) ;
                return candidate;
        }

        throw new Exception($"Unable to find supported format for {tiling} and {formatFeatureFlags}");
    }

    public AllocatedImage CreateImage(IAllocator allocator, ImageCreateInfo info, MemoryPropertyFlags propertyFlags)
    {
        vk.CreateImage(VkDevice, info, null, out var image);
        var allocInfo = new AllocationCreateInfo {Usage = propertyFlags};
        allocator.AllocateImage(image, allocInfo, out var allocation);
        return new AllocatedImage {Image = image, Allocation = allocation};
    }

    public static implicit operator Silk.NET.Vulkan.Device(Device d) => d.VkDevice;

    public void Dispose() => vk.DestroyDevice(VkDevice, null);
}