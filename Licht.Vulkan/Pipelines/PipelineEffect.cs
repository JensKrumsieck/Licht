using Silk.NET.Vulkan;

namespace Licht.Vulkan.Pipelines;

public readonly unsafe struct PipelineEffect : IDisposable
{
    private readonly VkGraphicsDevice _device;
    public readonly PipelineLayout EffectLayout;
    public readonly Silk.NET.Vulkan.DescriptorSetLayout[]? SetLayouts;
    public readonly ShaderStage[] Stages;

    private PipelineEffect(VkGraphicsDevice device, PipelineLayout effectLayout, Silk.NET.Vulkan.DescriptorSetLayout[]? setLayouts, ShaderStage[] stages)
    {
        _device = device;
        EffectLayout = effectLayout;
        SetLayouts = setLayouts;
        Stages = stages;
    }

    public static PipelineEffect BuildEffect(VkGraphicsDevice device, string vertexShader, string fragmentShader, Silk.NET.Vulkan.DescriptorSetLayout[]? setLayouts, PushConstantRange? pushRange = null)
    {
        var vertexStage = ShaderStage.FromFile(device, vertexShader, ShaderStageFlags.VertexBit);
        var fragmentStage = ShaderStage.FromFile(device, fragmentShader, ShaderStageFlags.FragmentBit);
        return BuildEffect(device, vertexStage, fragmentStage, setLayouts, pushRange);
    }
    
    public static PipelineEffect BuildEffect(VkGraphicsDevice device, uint[] vertexShader, uint[] fragmentShader, Silk.NET.Vulkan.DescriptorSetLayout[]? setLayouts, PushConstantRange? pushRange = null)
    {
        var vertexStage = ShaderStage.FromUInts(device, vertexShader, ShaderStageFlags.VertexBit);
        var fragmentStage = ShaderStage.FromUInts(device, fragmentShader, ShaderStageFlags.FragmentBit);
        return BuildEffect(device, vertexStage, fragmentStage, setLayouts, pushRange);
    }

    private static PipelineEffect BuildEffect(VkGraphicsDevice device, ShaderStage vertexStage, ShaderStage fragmentStage,
        Silk.NET.Vulkan.DescriptorSetLayout[]? setLayouts, PushConstantRange? pushRange = null)
    {
        var stages = new[] {vertexStage, fragmentStage};
        var pipelineLayout = CreatePipelineLayout(device, setLayouts, pushRange ?? new PushConstantRange());
        return new PipelineEffect(device, pipelineLayout, setLayouts, stages);
    }

    public static PipelineEffect BuildComputeEffect(VkGraphicsDevice device, string computeShader,
        Silk.NET.Vulkan.DescriptorSetLayout[]? setLayouts, PushConstantRange? pushRange = null)
    {
        var stage = ShaderStage.FromFile(device, computeShader, ShaderStageFlags.ComputeBit);
        return BuildComputeEffect(device, stage, setLayouts, pushRange);
    }
    
    private static PipelineEffect BuildComputeEffect(VkGraphicsDevice device, ShaderStage computeStage, Silk.NET.Vulkan.DescriptorSetLayout[]? setLayouts, PushConstantRange? pushRange = null)
    {
        var stages = new[] {computeStage};
        var pipelineLayout = CreatePipelineLayout(device, setLayouts, pushRange ?? new PushConstantRange());
        return new PipelineEffect(device, pipelineLayout, setLayouts, stages);
    }
    
    private static PipelineLayout CreatePipelineLayout(VkGraphicsDevice device, Silk.NET.Vulkan.DescriptorSetLayout[]? setLayouts, PushConstantRange pushRange)
    {
        fixed (Silk.NET.Vulkan.DescriptorSetLayout* pSetLayouts = setLayouts)
        {
            var layoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = setLayouts is not null ? (uint) setLayouts.Length : 0u,
                PSetLayouts = pSetLayouts,
                PushConstantRangeCount = pushRange.StageFlags != ShaderStageFlags.None ? 1 : 0u,
                PPushConstantRanges = &pushRange
            };
            return new PipelineLayout(device, layoutInfo);
        }
    }
    
    public void Dispose()
    {
        foreach (var stage in Stages) vk.DestroyShaderModule(_device, stage.ShaderModule, null);
        Array.Clear(Stages);
        vk.DestroyPipelineLayout(_device, EffectLayout, null);
    }
}