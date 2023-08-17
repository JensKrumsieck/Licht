using Catalyst.Engine.Graphics;
using ImGuiNET;
using Silk.NET.Vulkan;

namespace Catalyst.Engine.UI;

public class ImGuiContext : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly Sampler _fontSampler;
    private readonly DescriptorPool _descriptorPool;
    private readonly DescriptorSetLayout _descriptorSetLayout;
    private readonly DescriptorSet _descriptorSet;
    
    public ImGuiContext(GraphicsDevice device)
    {
        _device = device;
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        var io = ImGui.GetIO();
        io.Fonts.AddFontDefault();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        ImGui.StyleColorsDark();
        _descriptorPool = new DescriptorPool(device.Device, new[] {new DescriptorPoolSize(DescriptorType.CombinedImageSampler, 1)});
        var samplerCreateInfo = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            MipmapMode = SamplerMipmapMode.Linear,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            MinLod = -1000,
            MaxLod = 1000,
            MaxAnisotropy = 1.0f
        };
        _fontSampler = device.CreateSampler(samplerCreateInfo);
        _descriptorSetLayout = DescriptorSetLayoutBuilder
                                  .Start()
                                  .WithSampler(0, DescriptorType.CombinedImageSampler, ShaderStageFlags.FragmentBit, _fontSampler)
                                  .CreateOn(device.Device);
        _descriptorSet = _descriptorPool.AllocateDescriptorSet(new[] {_descriptorSetLayout});
        
    }

    public void Dispose()
    {
        _device.DestroySampler(_fontSampler);
        _descriptorPool.FreeDescriptorSet(_descriptorSet);
        _descriptorPool.Dispose();
        _descriptorSetLayout.Dispose();
        GC.SuppressFinalize(this);
    }
}