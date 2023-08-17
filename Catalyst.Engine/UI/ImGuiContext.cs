using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Catalyst.Engine.Graphics;
using Catalyst.Pipeline;
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
    private readonly ShaderEffect _shaderEffect;
    private readonly ShaderPass _shaderPass;
    
    public ImGuiContext(GraphicsDevice device, RenderPass renderPass)
    {
        _device = device;
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        var io = ImGui.GetIO();
        io.Fonts.AddFontDefault();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height);
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
        var pushRange = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.VertexBit,
            Offset = 0,
            Size = sizeof(float) * 4
        };
        _shaderEffect = ShaderEffect.BuildEffect(device.Device, UIShaders.VertexShader, UIShaders.FragmentShader,
            new[] {_descriptorSetLayout}, new[] {pushRange});
        var vertexInfo = new VertexInfo(
            new VertexInputBindingDescription[]
            {
                new(0, (uint) Unsafe.SizeOf<ImDrawVert>(), VertexInputRate.Vertex)
            },
            new VertexInputAttributeDescription[]
            {
                new(0, 0, Format.R32G32Sfloat, (uint) Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.pos))),
                new(1, 0, Format.R32G32Sfloat, (uint) Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.uv))),
                new(2, 0, Format.R8G8B8A8Unorm, (uint) Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.col)))
            });
        var passInfo = ShaderPassInfo.Default();
        _shaderPass = new ShaderPass(_device.Device, _shaderEffect, passInfo, vertexInfo, renderPass);
    }

    public void Dispose()
    {
        _device.DestroySampler(_fontSampler);
        _descriptorPool.FreeDescriptorSet(_descriptorSet);
        _descriptorPool.Dispose();
        _shaderPass.Dispose();
        _descriptorSetLayout.Dispose();
        _shaderEffect.Dispose();
        GC.SuppressFinalize(this);
    }
}