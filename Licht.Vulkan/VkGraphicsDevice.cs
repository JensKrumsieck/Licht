global using static Licht.Vulkan.VkGraphicsDevice;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Licht.Core;
using Licht.Vulkan.Extensions;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace Licht.Vulkan;

public sealed unsafe class VkGraphicsDevice : IDisposable
{
    public static readonly Vk vk = Vk.GetApi();
    public static Instance instance => vk.CurrentInstance!.Value;
    public static Device device => vk.CurrentDevice!.Value;
    
    private readonly ILogger? _logger;
    private readonly Instance _instance;
    private readonly DebugUtilsMessengerEXT _debugMessenger;
    private readonly PhysicalDevice _physicalDevice;
    private readonly Device _device;
    private Queue _mainQueue;
    private readonly uint _mainQueueIndex;
    private CommandPool _commandPool;
    
    private readonly ExtDebugUtils _debugUtils;
    
    
    public VkGraphicsDevice(ILogger? logger = null)
    {
        _logger = logger;
        
        //TODO: create context based on project settings files
        var enabledInstanceExtensions = new List<string>();
        var enabledLayers = new List<string>();
#if LGRAPHICSDEBUG
        enabledInstanceExtensions.Add(ExtDebugUtils.ExtensionName);
        enabledLayers.Add("VK_LAYER_KHRONOS_validation");
#endif
        var flags = InstanceCreateFlags.None;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
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

        var enabledDeviceExtensions = new List<string>();
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
    }
   
    public void Dispose()
    {
        _debugUtils.DestroyDebugUtilsMessenger(_instance, _debugMessenger, null);
        _debugUtils.Dispose();
        
        vk.DestroyInstance(_instance, null);
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