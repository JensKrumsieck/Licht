using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Catalyst;

public struct DeviceInfo
{
    public readonly List<string> EnabledExtensions = new() {KhrSwapchain.ExtensionName};
    public readonly List<string> EnabledLayers = new();
    public bool EnableDebug = true;
    public PhysicalDeviceFeatures DesiredFeatures = new() {SamplerAnisotropy = true, SampleRateShading = true};

    public DeviceInfo(){}

    public void Validate()
    {
        ValidateLayers();
        ValidateExtensions();
    }

    private void ValidateLayers()
    {
        if (EnableDebug 
            && !EnabledLayers.Contains(Constants.VkLayerKhronosValidation))
            EnabledLayers.Add(Constants.VkLayerKhronosValidation);
    }

    private void ValidateExtensions()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
            !EnabledExtensions.Contains(Constants.VkKhrPortabilitySubset))
            EnabledExtensions.Add(Constants.VkKhrPortabilitySubset);
    }
}