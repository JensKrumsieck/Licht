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
        _debugMessenger = new DebugUtilsMessengerEXT(_instance, debugInfo);
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
        _mainQueue = _device.GetQueue(_mainQueueIndex);

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

    public void WaitIdle() => _device.WaitIdle();
    public void WaitForFence(Fence fence) => _device.WaitForFence(fence);
    public Result WaitForQueue() => _mainQueue.WaitForQueue();
    public Result ResetFence(Fence fence) => _device.ResetFence(fence);
    public Result SubmitMainQueue(SubmitInfo submitInfo, Fence fence) => _mainQueue.QueueSubmit(submitInfo, fence);
    public CommandBuffer[] AllocateCommandBuffers(uint count) => _device.AllocateCommandBuffers(count, _commandPool);
    public void FreeCommandBuffers(CommandBuffer[] commandBuffers) => _device.FreeCommandBuffers(commandBuffers, _commandPool);
    public void FreeCommandBuffer(CommandBuffer commandBuffer) => _device.FreeCommandBuffer(commandBuffer, _commandPool);
    public Format FindFormat(Format[] candidates, ImageTiling tiling, FormatFeatureFlags formatFeatureFlags) => _physicalDevice.FindFormat(candidates, tiling, formatFeatureFlags);
    public AllocatedImage CreateImage(ImageCreateInfo info, MemoryPropertyFlags propertyFlags) => _device.CreateImage(_allocator, info, propertyFlags);
    public AllocatedBuffer CreateBuffer(ulong bufferSize, BufferUsageFlags usageFlags, MemoryPropertyFlags memoryFlags) => _device.CreateBuffer(_allocator, bufferSize, usageFlags, memoryFlags);
    public CommandBuffer BeginSingleTimeCommands() => _device.BeginSingleTimeCommands(_commandPool);
    public void EndSingleTimeCommands(CommandBuffer cmd) => _device.EndSingleTimeCommands(cmd, _commandPool, _mainQueue);

    public void Dispose()
    {
        _commandPool.Dispose();
        _device.Dispose();

        _debugMessenger.Dispose();

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