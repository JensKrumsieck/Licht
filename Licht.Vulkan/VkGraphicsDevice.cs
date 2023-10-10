global using static Licht.Vulkan.VkGraphicsDevice;
global using Semaphore = Silk.NET.Vulkan.Semaphore;
global using Buffer = Silk.NET.Vulkan.Buffer;

using System.Diagnostics;
using System.Runtime.InteropServices;
using Licht.GraphicsCore;
using Licht.Vulkan.Extensions;
using Licht.Vulkan.Memory;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Licht.Vulkan;

public sealed unsafe class VkGraphicsDevice : IDisposable
{
    public static readonly Vk vk = Vk.GetApi();

    public PhysicalDevice PhysicalDevice => _physicalDevice;
    public Instance Instance => _instance;
    public Device Device => _device;
    public Queue MainQueue => _mainQueue;
    public ILogger? Logger => _logger;
    
    private readonly ILogger? _logger;
    private readonly Instance _instance;
    private readonly IAllocator _allocator;
    private readonly DebugUtilsMessengerEXT _debugMessenger;
    private readonly PhysicalDevice _physicalDevice;
    private readonly Device _device;
    private readonly Queue _mainQueue;
    private readonly uint _mainQueueIndex;
    private readonly CommandPool _commandPool;
    
    private readonly ExtDebugUtils _debugUtils;
    
    
    public VkGraphicsDevice(IAllocator allocator, ILogger? logger = null)
    {
        _logger = logger;
        _allocator = allocator;
        
        //TODO: create context based on project settings files
        var enabledInstanceExtensions = new List<string>
        {
            KhrSurface.ExtensionName
        };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            enabledInstanceExtensions.Add(KhrWin32Surface.ExtensionName);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            enabledInstanceExtensions.Add(KhrXlibSurface.ExtensionName);
        
        var enabledLayers = new List<string>();
#if LGRAPHICSDEBUG
        enabledInstanceExtensions.Add(ExtDebugUtils.ExtensionName);
        enabledLayers.Add("VK_LAYER_KHRONOS_validation");
#endif
        var flags = InstanceCreateFlags.None;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            enabledInstanceExtensions.Add(ExtMetalSurface.ExtensionName);
            enabledInstanceExtensions.Add("VK_KHR_portability_enumeration");
            flags |= InstanceCreateFlags.EnumeratePortabilityBitKhr;
        }

        using var pPEnabledLayers = new ByteStringList(enabledLayers);
        using var pPEnabledInstanceExtensions = new ByteStringList(enabledInstanceExtensions);

        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            ApiVersion = Vk.Version13
        };
#if LGRAPHICSDEBUG
        var debugInfo = new DebugUtilsMessengerCreateInfoEXT
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt | DebugUtilsMessageSeverityFlagsEXT.InfoBitExt,
            MessageType = DebugUtilsMessageTypeFlagsEXT.ValidationBitExt | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt | DebugUtilsMessageTypeFlagsEXT.DeviceAddressBindingBitExt,
            PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT) DebugCallback
        };
#endif
        var instanceInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            Flags = flags,
            EnabledLayerCount = pPEnabledLayers.Count,
            PpEnabledLayerNames = pPEnabledLayers,
            EnabledExtensionCount = pPEnabledInstanceExtensions.Count,
            PpEnabledExtensionNames = pPEnabledInstanceExtensions,
            PApplicationInfo = &appInfo,
#if LGRAPHICSDEBUG
            PNext = &debugInfo,
#endif
        };
        vk.CreateInstance(instanceInfo, null, out _instance).Validate(_logger);
        _logger?.LogDebug("Enabled Layers: {Layers}", string.Join(", ", enabledLayers));
        _logger?.LogDebug("Enabled Instance Extensions: {InstanceExtensions}", string.Join(", ", enabledInstanceExtensions));
        
#if LGRAPHICSDEBUG
        if(!vk.TryGetInstanceExtension(_instance, out _debugUtils))
            _logger?.LogError($"Could not get instance extension {ExtDebugUtils.ExtensionName}!");
        _debugUtils.CreateDebugUtilsMessenger(_instance, debugInfo, null, out _debugMessenger).Validate(_logger);
#endif

        var devices = vk.GetPhysicalDevices(_instance);
        _physicalDevice = devices.FirstOrDefault(gpu =>
        {
            vk.GetPhysicalDeviceProperties(gpu, out var p);
            return p.DeviceType == PhysicalDeviceType.DiscreteGpu;
        });
        if (_physicalDevice.Handle == 0) _physicalDevice = devices.First();
        vk.GetPhysicalDeviceProperties(_physicalDevice, out var properties);
        _logger?.LogDebug("{DeviceName}", new ByteString(properties.DeviceName));

        var enabledDeviceExtensions = new List<string>{ KhrSwapchain.ExtensionName };
        if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) 
            enabledDeviceExtensions.Add("VK_KHR_portability_subset");
        var pPEnabledDeviceExtensions = new ByteStringList(enabledDeviceExtensions);

        var defaultPriority = 1.0f;
        var queueFamilyCount = 0u;
        vk.GetPhysicalDeviceQueueFamilyProperties(_physicalDevice, ref queueFamilyCount, null);
        _logger?.LogTrace("Found {QueueFamilyCount} device queues", queueFamilyCount);
        var queueFamilies = new QueueFamilyProperties[queueFamilyCount];
        fixed (QueueFamilyProperties* pQueueFamilies = queueFamilies)
            vk.GetPhysicalDeviceQueueFamilyProperties(_physicalDevice, ref queueFamilyCount, pQueueFamilies);

        //TODO: separate queues for different tasks?
        for (var i = 0u; i < queueFamilyCount; i++)
        {
            if (queueFamilies[i].QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                _mainQueueIndex = i;
                break;
            }
        }

        var queueInfo = new DeviceQueueCreateInfo
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueCount = 1,
            QueueFamilyIndex = _mainQueueIndex,
            PQueuePriorities = &defaultPriority
        };

        var deviceInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            EnabledLayerCount = pPEnabledLayers.Count,
            PpEnabledLayerNames = pPEnabledLayers,
            EnabledExtensionCount = pPEnabledDeviceExtensions.Count,
            PpEnabledExtensionNames = pPEnabledDeviceExtensions,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &queueInfo
        };
        vk.CreateDevice(_physicalDevice, deviceInfo, null, out _device).Validate(_logger);
        _logger?.LogDebug("Enabled Device Extensions: {DeviceExtensions}", string.Join(", ", enabledDeviceExtensions));
        vk.GetDeviceQueue(_device, _mainQueueIndex, 0, out _mainQueue);

        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = _mainQueueIndex,
            //TODO: find out what these flags actually mean
            Flags = CommandPoolCreateFlags.TransientBit | CommandPoolCreateFlags.ResetCommandBufferBit
        };
        vk.CreateCommandPool(_device, poolInfo, null, out _commandPool).Validate(_logger);
        
        //bind to allocator
        allocator.Bind(this);
    }

    public void WaitIdle() => vk.DeviceWaitIdle(_device);
    public void WaitForFence(Fence fence) => vk.WaitForFences(_device, 1u, fence, true, ulong.MaxValue);
    public Result WaitForQueue() => vk.QueueWaitIdle(_mainQueue);
    public Result ResetFence(Fence fence) => vk.ResetFences(_device, 1, fence);
    public Result SubmitMainQueue(SubmitInfo submitInfo, Fence fence) => vk.QueueSubmit(_mainQueue, 1, submitInfo, fence);
    public VkCommandBuffer[] AllocateCommandBuffers(uint count)
    {
        var commandBuffers = new CommandBuffer[count];
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = _commandPool,
            CommandBufferCount = count
        };
        fixed (void* pCommandBuffers = commandBuffers)
            vk.AllocateCommandBuffers(_device, allocInfo, (CommandBuffer*)pCommandBuffers).Validate(_logger);
        var vkCommandBuffers = new VkCommandBuffer[count];
        for (var i = 0; i < vkCommandBuffers.Length; i++) vkCommandBuffers[i] = new VkCommandBuffer(commandBuffers[i]);
        return vkCommandBuffers;
    }
    public void FreeCommandBuffers(VkCommandBuffer[] commandBuffers) =>
        vk.FreeCommandBuffers(_device, _commandPool, (uint) commandBuffers.Length,
            Array.ConvertAll(commandBuffers, cmd => (CommandBuffer) cmd));
    public void FreeCommandBuffer(CommandBuffer commandBuffer) =>
        vk.FreeCommandBuffers(_device, _commandPool, 1, commandBuffer);
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
        _logger?.LogError("Unable to find supported format for {Tiling} and {FormatFeatureFlags}", tiling, formatFeatureFlags);
        throw new Exception($"Unable to find supported format for {tiling} and {formatFeatureFlags}");
    }
    public AllocatedImage CreateImage(ImageCreateInfo info, MemoryPropertyFlags propertyFlags)
    {
        vk.CreateImage(_device, info, null, out var image).Validate(_logger);
        var allocInfo = new AllocationCreateInfo {Usage = propertyFlags};
        _allocator.AllocateImage(image, allocInfo, out var allocation);
        return new AllocatedImage(image, allocation);
    }
    public void DestroyImage(AllocatedImage image)
    {
        vk.DestroyImage(_device, image.Image, null);
        image.Allocation.Dispose();
    }
    public ImageView CreateImageView(ImageViewCreateInfo info)
    {
        vk.CreateImageView(_device, info, null, out var imageView).Validate(_logger);
        return imageView;
    }
    public void DestroyImageView(ImageView view) => vk.DestroyImageView(_device, view, null);
    public Sampler CreateSampler(SamplerCreateInfo createInfo)
    {
        vk.CreateSampler(_device, createInfo, default, out var sampler).Validate(_logger);
        return sampler;
    }
    public void DestroySampler(Sampler sampler) => vk.DestroySampler(_device, sampler, null);
    public AllocatedBuffer CreateBuffer(ulong bufferSize, BufferUsageFlags usageFlags, MemoryPropertyFlags memoryFlags)
    {
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = bufferSize,
            Usage = usageFlags,
            SharingMode = SharingMode.Exclusive
        };
        vk.CreateBuffer(_device, bufferInfo, null, out var buffer).Validate(_logger);
        var allocInfo = new AllocationCreateInfo {Usage = memoryFlags};
        _allocator.AllocateBuffer(buffer, allocInfo, out var allocation);
        return new AllocatedBuffer(buffer, allocation);
    }
    public VkCommandBuffer BeginSingleTimeCommands()
    {
        var cmd = AllocateCommandBuffers(1)[0];
        cmd.Begin();
        return cmd;
    }
    public void EndSingleTimeCommands(VkCommandBuffer cmd)
    {
        cmd.End();
        var commandBuffer = (CommandBuffer) cmd;
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };
        SubmitMainQueue(submitInfo, default);
        WaitForQueue();
        FreeCommandBuffer(cmd);
    }

    public static implicit operator Device(VkGraphicsDevice d) => d._device;
    public static implicit operator Instance(VkGraphicsDevice d) => d._instance;
    public static implicit operator (Instance instance, Device device)(VkGraphicsDevice d) => (d._instance, d._device);
    
    public void Dispose()
    {
        WaitIdle();
        _allocator.Dispose();
        vk.DestroyCommandPool(_device, _commandPool, null);
        vk.DestroyDevice(_device, null);
        
        _debugUtils.DestroyDebugUtilsMessenger(_instance, _debugMessenger, null);
        _debugUtils.Dispose();
        
        vk.DestroyInstance(_instance, null);
        vk.Dispose();
    }
    
    [StackTraceHidden]
    private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT severityFlags,
        DebugUtilsMessageTypeFlagsEXT messageTypeFlags,
        DebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* userData)
    {
        var message = new ByteString(pCallbackData->PMessage);
        if(_logger is null) Console.WriteLine("[{0}]: {1}: {2}",severityFlags.GetLogLevel(), messageTypeFlags, message);
        else _logger?.Log(severityFlags.GetLogLevel(), "{MessageTypeFlags}: {Message}", messageTypeFlags, message);
        return Vk.False;
    }
}