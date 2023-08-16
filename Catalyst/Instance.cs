using Catalyst.Tools;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace Catalyst;

public readonly unsafe struct Instance : IDisposable
{
    private readonly ExtDebugUtils _extDebugUtils;
    
    public readonly  Silk.NET.Vulkan.Instance VkInstance;
    public IntPtr Handle => VkInstance.Handle;
    private readonly DebugUtilsMessengerEXT _debugUtilsMessenger;

    public Instance() : this(new InstanceInfo()){}
    
    public Instance(InstanceInfo info)
    {
        info.Validate();
        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = new ByteString(info.ApplicationName),
            ApplicationVersion = info.ApplicationVersion,
            PEngineName = new ByteString(info.EngineName),
            EngineVersion = info.EngineVersion,
            ApiVersion = info.ApiVersion
        };
        var extensions = new ByteStringList(info.EnabledExtensions);
        var layers = new ByteStringList(info.EnabledLayers);
        var createInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo,
            EnabledExtensionCount = extensions.Count,
            PpEnabledExtensionNames = extensions,
            EnabledLayerCount = layers.Count,
            PpEnabledLayerNames = layers,
            Flags = info.Flags
        };

        if (info.EnableDebug)
        {
            var debugCreateInfo = PrepareDebugCreateInfo();
            createInfo.PNext = &debugCreateInfo;
        }

        vk.CreateInstance(createInfo, null, out VkInstance).Validate();
        if (!vk.TryGetInstanceExtension(VkInstance, out _extDebugUtils))
            throw new Exception($"[Vulkan] Could not find extension {ExtDebugUtils.ExtensionName}");
        _extDebugUtils.CreateDebugUtilsMessenger(VkInstance, PrepareDebugCreateInfo(), null, out _debugUtilsMessenger).Validate();
    }

    public PhysicalDevice SelectPhysicalDevice(Func<PhysicalDevice, int>? scoringFunction)
    {
        var physicalDevices = vk.GetPhysicalDevices(VkInstance).ToArray();
        return SelectorTools.SelectByScore(physicalDevices, scoringFunction);
    }
    
    private static DebugUtilsMessengerCreateInfoEXT PrepareDebugCreateInfo() => new DebugUtilsMessengerCreateInfoEXT
    {
        SType = StructureType.DebugUtilsMessengerCreateInfoExt,
        MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt,
        MessageType = DebugUtilsMessageTypeFlagsEXT.ValidationBitExt | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt | DebugUtilsMessageTypeFlagsEXT.DeviceAddressBindingBitExt,
        PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT) DebugTools.DefaultDebugCallback
    };

    public static implicit operator Silk.NET.Vulkan.Instance(Instance i) => i.VkInstance;
    
    public void Dispose()
    {
        _extDebugUtils.DestroyDebugUtilsMessenger(VkInstance, _debugUtilsMessenger, null);
        vk.DestroyInstance(VkInstance, null);
        _extDebugUtils.Dispose();
    }
}