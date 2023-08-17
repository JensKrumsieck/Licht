using Catalyst.Tools;
using Silk.NET.Vulkan;

namespace Catalyst.Pipeline;

public readonly unsafe struct ShaderEffect : IDisposable
{
    private readonly Device _device;
    public readonly PipelineLayout EffectLayout;
    public readonly DescriptorSetLayout[] SetLayouts;
    public readonly ShaderStage[] Stages;

    private ShaderEffect(Device device, PipelineLayout effectLayout, DescriptorSetLayout[] setLayouts, ShaderStage[] stages)
    {
        _device = device;
        EffectLayout = effectLayout;
        SetLayouts = setLayouts;
        Stages = stages;
    }

    public static ShaderEffect BuildEffect(Device device, string vertexShader, string fragmentShader, DescriptorSetLayout[] setLayouts, PushConstantRange[]? pushRanges = null)
    {
        var vertexStage = ShaderStage.FromFile(device, vertexShader, ShaderStageFlags.VertexBit);
        var fragmentStage = ShaderStage.FromFile(device, fragmentShader, ShaderStageFlags.FragmentBit);
        return BuildEffect(device, vertexStage, fragmentStage, setLayouts, pushRanges);
    }
    
    public static ShaderEffect BuildEffect(Device device, uint[] vertexShader, uint[] fragmentShader, DescriptorSetLayout[] setLayouts, PushConstantRange[]? pushRanges = null)
    {
        var vertexStage = ShaderStage.FromUInts(device, vertexShader, ShaderStageFlags.VertexBit);
        var fragmentStage = ShaderStage.FromUInts(device, fragmentShader, ShaderStageFlags.FragmentBit);
        return BuildEffect(device, vertexStage, fragmentStage, setLayouts, pushRanges);
    }

    private static ShaderEffect BuildEffect(Device device, ShaderStage vertexStage, ShaderStage fragmentStage,
        DescriptorSetLayout[] setLayouts, PushConstantRange[]? pushRanges = null)
    {
        var stages = new[] {vertexStage, fragmentStage};
        var pipelineLayout = CreatePipelineLayout(device, setLayouts.AsArray<DescriptorSetLayout, Silk.NET.Vulkan.DescriptorSetLayout>(), pushRanges);
        return new ShaderEffect(device, pipelineLayout, setLayouts, stages);
    }
    
    private static unsafe PipelineLayout CreatePipelineLayout(Device device, Silk.NET.Vulkan.DescriptorSetLayout[] setLayouts, PushConstantRange[]? pushRanges)
    {
        fixed (Silk.NET.Vulkan.DescriptorSetLayout* pSetLayouts = setLayouts)
        fixed (PushConstantRange* pPushRanges = pushRanges)
        {
            var layoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = (uint) setLayouts.Length,
                PSetLayouts = pSetLayouts,
                PushConstantRangeCount = pushRanges is not null ? (uint) pushRanges.Length : 0u,
                PPushConstantRanges = pPushRanges
            };
            vk.CreatePipelineLayout(device, layoutInfo, null, out var pipelineLayout).Validate();
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
