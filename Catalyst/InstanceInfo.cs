using System.Runtime.InteropServices;
using Catalyst.Tools;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Catalyst;

public struct InstanceInfo
{
    public string ApplicationName = "Catalyst";
    public string EngineName = "No Engine";
    public Version32 ApplicationVersion = Vk.Version10;
    public Version32 EngineVersion = Vk.Version10;
    public Version32 ApiVersion = Vk.Version13;
    public readonly List<string> EnabledExtensions = new() {KhrSurface.ExtensionName};
    public readonly List<string> EnabledLayers = new();
    public InstanceCreateFlags Flags = InstanceCreateFlags.None;
    public bool EnableDebug = true;

    public InstanceInfo() { }

    public void Validate()
    {
        ValidateLayers();
        ValidateExtensions();
    }

    private void ValidateLayers()
    {
        if(EnableDebug && !EnabledLayers.Contains(Constants.VkLayerKhronosValidation))
            EnabledLayers.Add(Constants.VkLayerKhronosValidation);
    }

    private unsafe void ValidateExtensions()
    {
        var propertyCount = 0u;
        vk.EnumerateInstanceExtensionProperties("", ref propertyCount, null);
        var availableExtensionProperties = new ExtensionProperties[propertyCount];
        fixed (ExtensionProperties* pAvailableExtensionProperties = availableExtensionProperties)
            vk.EnumerateInstanceExtensionProperties("", ref propertyCount, pAvailableExtensionProperties);
        var availableExtensions = new string[propertyCount];
        
        for (var i = 0; i < availableExtensionProperties.Length; i++)
        {
            fixed (byte* pExtensionName = availableExtensionProperties[i].ExtensionName)
                availableExtensions[i] = new ByteString(pExtensionName);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (!Flags.HasFlag(InstanceCreateFlags.EnumeratePortabilityBitKhr))
                Flags |= InstanceCreateFlags.EnumeratePortabilityBitKhr;
            if(!EnabledExtensions.Contains(Constants.VkKhrPortabilityEnumeration))
                EnabledExtensions.Add(Constants.VkKhrPortabilityEnumeration);
            if(EnabledExtensions.Contains(KhrSurface.ExtensionName) 
               && !EnabledExtensions.Contains(ExtMetalSurface.ExtensionName))
                EnabledExtensions.Add(ExtMetalSurface.ExtensionName);
        }
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
           && EnabledExtensions.Contains(KhrSurface.ExtensionName) 
           && !EnabledExtensions.Contains(KhrWin32Surface.ExtensionName))
            EnabledExtensions.Add(KhrWin32Surface.ExtensionName);
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) 
            && EnabledExtensions.Contains(KhrSurface.ExtensionName) )
        {
            if(availableExtensions.Contains(KhrXlibSurface.ExtensionName)
               && !EnabledExtensions.Contains(KhrXlibSurface.ExtensionName))
                EnabledExtensions.Add(KhrXlibSurface.ExtensionName);
            if(availableExtensions.Contains(KhrXcbSurface.ExtensionName)
               && !EnabledExtensions.Contains(KhrXcbSurface.ExtensionName))
                EnabledExtensions.Add(KhrXcbSurface.ExtensionName);
            if(availableExtensions.Contains(KhrWaylandSurface.ExtensionName)
               && !EnabledExtensions.Contains(KhrWaylandSurface.ExtensionName))
                EnabledExtensions.Add(KhrWaylandSurface.ExtensionName);
        }
        
        if(EnableDebug && !EnabledExtensions.Contains(ExtDebugUtils.ExtensionName))
            EnabledExtensions.Add(ExtDebugUtils.ExtensionName);
    }
}