using Licht.Vulkan.Extensions;
using Silk.NET.Vulkan;

namespace Licht.Vulkan.Pipelines;

public readonly unsafe struct ShaderEffect : IDisposable
{
    private readonly VkGraphicsDevice _device;
    public readonly PipelineLayout EffectLayout;
    public readonly DescriptorSetLayout[]? SetLayouts;
    public readonly ShaderStage[] Stages;

    private ShaderEffect(VkGraphicsDevice device, PipelineLayout effectLayout, DescriptorSetLayout[]? setLayouts, ShaderStage[] stages)
    {
        _device = device;
        EffectLayout = effectLayout;
        SetLayouts = setLayouts;
        Stages = stages;
    }

    public static ShaderEffect BuildEffect(VkGraphicsDevice device, string vertexShader, string fragmentShader, DescriptorSetLayout[]? setLayouts, PushConstantRange? pushRange = null)
    {
        var vertexStage = ShaderStage.FromFile(device, vertexShader, ShaderStageFlags.VertexBit);
        var fragmentStage = ShaderStage.FromFile(device, fragmentShader, ShaderStageFlags.FragmentBit);
        return BuildEffect(device, vertexStage, fragmentStage, setLayouts, pushRange);
    }
    
    public static ShaderEffect BuildEffect(VkGraphicsDevice device, uint[] vertexShader, uint[] fragmentShader, DescriptorSetLayout[]? setLayouts, PushConstantRange? pushRange = null)
    {
        var vertexStage = ShaderStage.FromUInts(device, vertexShader, ShaderStageFlags.VertexBit);
        var fragmentStage = ShaderStage.FromUInts(device, fragmentShader, ShaderStageFlags.FragmentBit);
        return BuildEffect(device, vertexStage, fragmentStage, setLayouts, pushRange);
    }

    private static ShaderEffect BuildEffect(VkGraphicsDevice device, ShaderStage vertexStage, ShaderStage fragmentStage,
        DescriptorSetLayout[]? setLayouts, PushConstantRange? pushRange = null)
    {
        var stages = new[] {vertexStage, fragmentStage};
        var pipelineLayout = CreatePipelineLayout(device, setLayouts, pushRange ?? new PushConstantRange());
        return new ShaderEffect(device, pipelineLayout, setLayouts, stages);
    }

    public static ShaderEffect BuildComputeEffect(VkGraphicsDevice device, string computeShader,
        DescriptorSetLayout[]? setLayouts, PushConstantRange? pushRange = null)
    {
        var stage = ShaderStage.FromFile(device, computeShader, ShaderStageFlags.ComputeBit);
        return BuildComputeEffect(device, stage, setLayouts, pushRange);
    }
    
    private static ShaderEffect BuildComputeEffect(VkGraphicsDevice device, ShaderStage computeStage, DescriptorSetLayout[]? setLayouts, PushConstantRange? pushRange = null)
    {
        var stages = new[] {computeStage};
        var pipelineLayout = CreatePipelineLayout(device, setLayouts, pushRange ?? new PushConstantRange());
        return new ShaderEffect(device, pipelineLayout, setLayouts, stages);
    }
    
    private static PipelineLayout CreatePipelineLayout(VkGraphicsDevice device, DescriptorSetLayout[]? setLayouts, PushConstantRange pushRange)
    {
        fixed (DescriptorSetLayout* pSetLayouts = setLayouts)
        {
            var layoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = setLayouts is not null ? (uint) setLayouts.Length : 0u,
                PSetLayouts = pSetLayouts,
                PushConstantRangeCount = pushRange.StageFlags != ShaderStageFlags.None ? 1 : 0u,
                PPushConstantRanges = &pushRange
            };
            vk.CreatePipelineLayout(device, layoutInfo, null, out var pipelineLayout).Validate(device.Logger);
            return pipelineLayout;
        }
    }
    
    public void Dispose()
    {
        foreach (var stage in Stages) vk.DestroyShaderModule(_device, stage.ShaderModule, null);
        Array.Clear(Stages);
        vk.DestroyPipelineLayout(_device, EffectLayout, null);
    }
}