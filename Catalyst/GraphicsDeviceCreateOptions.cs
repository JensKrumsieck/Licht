using System.Runtime.InteropServices;
using Catalyst.Tools;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Catalyst;

public struct GraphicsDeviceCreateOptions
{
    public string ApplicationName = "Catalyst";
    public string EngineName = "No Engine";
    public Version32 ApplicationVersion = Vk.Version10;
    public Version32 EngineVersion = Vk.Version10;
    public Version32 ApiVersion = Vk.Version13;
    public InstanceCreateFlags Flags = InstanceCreateFlags.None;
    public readonly List<string> EnabledInstanceExtensions = new() { KhrSurface.ExtensionName };
    public readonly List<string> EnabledDeviceExtensions = new() { KhrSwapchain.ExtensionName };
    public readonly List<string> EnabledLayers = new();
    public bool EnableDebug = true;
    public Func<PhysicalDevice, int>? PhysicalDeviceSelector = null;
    public PhysicalDeviceFeatures DesiredDeviceFeatures = new() { SamplerAnisotropy = true, SampleRateShading = true };
    public CommandPoolCreateFlags CommandPoolCreateFlags = CommandPoolCreateFlags.TransientBit | CommandPoolCreateFlags.ResetCommandBufferBit;
    
    public GraphicsDeviceCreateOptions() { }

    public void Validate()
    {
        ValidateLayers();
        ValidateInstanceExtensions();
        ValidateDeviceExtensions();
    }
    
    private void ValidateLayers()
    {
        if(EnableDebug && !EnabledLayers.Contains(Constants.VkLayerKhronosValidation))
            EnabledLayers.Add(Constants.VkLayerKhronosValidation);
    }
    
    private unsafe void ValidateInstanceExtensions()
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
            if(!EnabledInstanceExtensions.Contains(Constants.VkKhrPortabilityEnumeration))
                EnabledInstanceExtensions.Add(Constants.VkKhrPortabilityEnumeration);
            if(EnabledInstanceExtensions.Contains(KhrSurface.ExtensionName) 
               && !EnabledInstanceExtensions.Contains(ExtMetalSurface.ExtensionName))
                EnabledInstanceExtensions.Add(ExtMetalSurface.ExtensionName);
        }
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
           && EnabledInstanceExtensions.Contains(KhrSurface.ExtensionName) 
           && !EnabledInstanceExtensions.Contains(KhrWin32Surface.ExtensionName))
            EnabledInstanceExtensions.Add(KhrWin32Surface.ExtensionName);
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) 
            && EnabledInstanceExtensions.Contains(KhrSurface.ExtensionName) )
        {
            if(availableExtensions.Contains(KhrXlibSurface.ExtensionName)
               && !EnabledInstanceExtensions.Contains(KhrXlibSurface.ExtensionName))
                EnabledInstanceExtensions.Add(KhrXlibSurface.ExtensionName);
            if(availableExtensions.Contains(KhrXcbSurface.ExtensionName)
               && !EnabledInstanceExtensions.Contains(KhrXcbSurface.ExtensionName))
                EnabledInstanceExtensions.Add(KhrXcbSurface.ExtensionName);
            if(availableExtensions.Contains(KhrWaylandSurface.ExtensionName)
               && !EnabledInstanceExtensions.Contains(KhrWaylandSurface.ExtensionName))
                EnabledInstanceExtensions.Add(KhrWaylandSurface.ExtensionName);
        }
        
        if(EnableDebug && !EnabledInstanceExtensions.Contains(ExtDebugUtils.ExtensionName))
            EnabledInstanceExtensions.Add(ExtDebugUtils.ExtensionName);
    }
    
    private void ValidateDeviceExtensions()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
            !EnabledDeviceExtensions.Contains(Constants.VkKhrPortabilitySubset))
            EnabledDeviceExtensions.Add(Constants.VkKhrPortabilitySubset);
    }
}
