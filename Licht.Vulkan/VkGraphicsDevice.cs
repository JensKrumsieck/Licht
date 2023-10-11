global using static Licht.Vulkan.VkGraphicsDevice;
global using Semaphore = Licht.Vulkan.Semaphore;
global using Buffer = Licht.Vulkan.Buffer;

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

    public static implicit operator Device(VkGraphicsDevice d) => d._device;
    public static implicit operator Silk.NET.Vulkan.Device(VkGraphicsDevice d) => d._device;

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
        _instance = new Instance(instanceInfo);
        _logger?.LogDebug("Enabled Layers: {Layers}", string.Join(", ", enabledLayers));
        _logger?.LogDebug("Enabled Instance Extensions: {InstanceExtensions}", string.Join(", ", enabledInstanceExtensions));
        
#if LGRAPHICSDEBUG
        if(!vk.TryGetInstanceExtension(_instance, out _debugUtils))
            _logger?.LogError($"Could not get instance extension {ExtDebugUtils.ExtensionName}!");
        _debugUtils.CreateDebugUtilsMessenger(_instance, debugInfo, null, out _debugMessenger).Validate(_logger);
#endif

        _physicalDevice = _instance.SelectPhysicalDevice();
        var properties = _physicalDevice.GetProperties();
        _logger?.LogDebug("{DeviceName}", new ByteString(properties.DeviceName));

        var enabledDeviceExtensions = new List<string>{ KhrSwapchain.ExtensionName };
        if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) 
            enabledDeviceExtensions.Add("VK_KHR_portability_subset");
        var pPEnabledDeviceExtensions = new ByteStringList(enabledDeviceExtensions);

        var defaultPriority = 1.0f;
        var queueFamilies = _physicalDevice.GetQueueFamilyProperties();
        var queueFamilyCount = queueFamilies.Length;

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
        _device = new Device(_physicalDevice, deviceInfo);
        _logger?.LogDebug("Enabled Device Extensions: {DeviceExtensions}", string.Join(", ", enabledDeviceExtensions));
        vk.GetDeviceQueue(_device, _mainQueueIndex, 0, out _mainQueue);

        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = _mainQueueIndex,
            //TODO: find out what these flags actually mean
            Flags = CommandPoolCreateFlags.TransientBit | CommandPoolCreateFlags.ResetCommandBufferBit
        };
        _commandPool = new CommandPool(_device, poolInfo);
        //bind to allocator
        allocator.Bind(this);
    }

    public void WaitIdle() => vk.DeviceWaitIdle(_device);
    public void WaitForFence(Fence fence) => vk.WaitForFences(_device, 1u, fence, true, ulong.MaxValue);
    public Result WaitForQueue() => vk.QueueWaitIdle(_mainQueue);
    public Result ResetFence(Fence fence) => vk.ResetFences(_device, 1, fence);
    public Result SubmitMainQueue(SubmitInfo submitInfo, Fence fence) => vk.QueueSubmit(_mainQueue, 1, submitInfo, fence);
    public CommandBuffer[] AllocateCommandBuffers(uint count)
    {
        var commandBuffers = new Silk.NET.Vulkan.CommandBuffer[count];
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = _commandPool,
            CommandBufferCount = count
        };
        fixed (Silk.NET.Vulkan.CommandBuffer* pCommandBuffers = commandBuffers)
            vk.AllocateCommandBuffers(_device, allocInfo, pCommandBuffers).Validate(_logger);
        var vkCommandBuffers = new CommandBuffer[count];
        for (var i = 0; i < vkCommandBuffers.Length; i++) vkCommandBuffers[i] = commandBuffers[i];
        return vkCommandBuffers;
    }
    public void FreeCommandBuffers(CommandBuffer[] commandBuffers) =>
        vk.FreeCommandBuffers(_device, _commandPool, (uint) commandBuffers.Length,
            Array.ConvertAll(commandBuffers, cmd => (Silk.NET.Vulkan.CommandBuffer) cmd));
    public void FreeCommandBuffer(CommandBuffer commandBuffer) =>
        vk.FreeCommandBuffers(_device, _commandPool, 1, commandBuffer);
    public Format FindFormat(Format[] candidates, ImageTiling tiling, FormatFeatureFlags formatFeatureFlags) => _physicalDevice.FindFormat(candidates, tiling, formatFeatureFlags);
    public AllocatedImage CreateImage(ImageCreateInfo info, MemoryPropertyFlags propertyFlags)
    {
        var image = new Image(_device, info);
        var allocInfo = new AllocationCreateInfo {Usage = propertyFlags};
        _allocator.AllocateImage(image, allocInfo, out var allocation);
        return new AllocatedImage(image, allocation);
    }
    public void DestroyImage(AllocatedImage image)
    {
        vk.DestroyImage(_device, image.Image, null);
        image.Allocation.Dispose();
    }
    public AllocatedBuffer CreateBuffer(ulong bufferSize, BufferUsageFlags usageFlags, MemoryPropertyFlags memoryFlags)
    {
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = bufferSize,
            Usage = usageFlags,
            SharingMode = SharingMode.Exclusive
        };
        var buffer = new Buffer(_device, bufferInfo);
        var allocInfo = new AllocationCreateInfo {Usage = memoryFlags};
        _allocator.AllocateBuffer(buffer, allocInfo, out var allocation);
        return new AllocatedBuffer(buffer, allocation);
    }
    public CommandBuffer BeginSingleTimeCommands()
    {
        var cmd = AllocateCommandBuffers(1)[0];
        cmd.Begin();
        return cmd;
    }
    public void EndSingleTimeCommands(CommandBuffer cmd)
    {
        cmd.End();
        var commandBuffer = (Silk.NET.Vulkan.CommandBuffer)cmd;
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

    public void Dispose()
    {
        WaitIdle();
        _allocator.Dispose();
        _commandPool.Dispose();
        _device.Dispose();
        
        _debugUtils.DestroyDebugUtilsMessenger(_instance, _debugMessenger, null);
        _debugUtils.Dispose();

        _instance.Dispose();
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