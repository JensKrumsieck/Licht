using Catalyst.Tools;
using Silk.NET.Vulkan;

namespace Catalyst.Pipeline;

public readonly unsafe struct ShaderPass : IDisposable, IConvertibleTo<Silk.NET.Vulkan.Pipeline>
{
    private readonly Device _device;
    public readonly ShaderEffect Effect;
    public readonly Silk.NET.Vulkan.Pipeline Pipeline;
    public PipelineLayout Layout => Effect.EffectLayout;
    
    public ShaderPass(Device device, ShaderEffect effect, ShaderPassInfo passInfo, VertexInfo vertexInfo, RenderPass renderPass)
    {
        _device = device;
        Effect = effect;
        var shaderStages = stackalloc PipelineShaderStageCreateInfo[effect.Stages.Length];
        for (var i = 0; i < effect.Stages.Length; i++) shaderStages[i] = effect.Stages[i].GetShaderStageCreateInfo();
        fixed(VertexInputAttributeDescription* pAttributeDescriptions = vertexInfo.AttributeDescriptions)
        fixed (VertexInputBindingDescription* pBindingDescriptions = vertexInfo.BindingDescriptions)
        {
            var vertexInput = new PipelineVertexInputStateCreateInfo
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexAttributeDescriptionCount = vertexInfo.AttributeDescriptions is not null ? (uint) vertexInfo.AttributeDescriptions!.Length : 0u,
                PVertexAttributeDescriptions = pAttributeDescriptions,
                VertexBindingDescriptionCount = vertexInfo.BindingDescriptions is not null ? (uint) vertexInfo.BindingDescriptions!.Length : 0u,
                PVertexBindingDescriptions = pBindingDescriptions
            };
            var colorBlendInfo = new PipelineColorBlendStateCreateInfo
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOpEnable = false,
                LogicOp = LogicOp.Copy,
                AttachmentCount = 1,
                PAttachments = &passInfo.ColorBlendAttachment
            };
            var createInfo = new GraphicsPipelineCreateInfo
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = 2,
                PStages = shaderStages,
                PVertexInputState = &vertexInput,
                PInputAssemblyState = &passInfo.InputAssemblyInfo,
                PViewportState = &passInfo.ViewportInfo,
                PRasterizationState = &passInfo.RasterizationInfo,
                PMultisampleState = &passInfo.MultisampleInfo,
                PColorBlendState = &colorBlendInfo,
                PDepthStencilState = &passInfo.DepthStencilInfo,
                PDynamicState = &passInfo.DynamicStateInfo,
                Layout = effect.EffectLayout,
                RenderPass = renderPass,
                Subpass = passInfo.SubPass,
                BasePipelineIndex = -1,
                BasePipelineHandle = default
            };
            vk.CreateGraphicsPipelines(device, default, 1, createInfo, null, out Pipeline);
        }
    }
    public static implicit operator Silk.NET.Vulkan.Pipeline(ShaderPass p) => p.Pipeline;
    public Silk.NET.Vulkan.Pipeline Convert() => Pipeline;
    public void Dispose() => vk.DestroyPipeline(_device, Pipeline, null);
}
