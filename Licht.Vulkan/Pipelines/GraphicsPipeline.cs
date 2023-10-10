using Licht.Vulkan.Extensions;
using Silk.NET.Vulkan;

namespace Licht.Vulkan.Pipelines;

public sealed unsafe class GraphicsPipeline : IDisposable
{
    private readonly VkGraphicsDevice _device;
    public readonly ShaderEffect Effect;
    public readonly Pipeline Pipeline;
    public PipelineLayout Layout => Effect.EffectLayout;
    public GraphicsPipeline(VkGraphicsDevice device, ShaderEffect effect, ShaderPassDescription description,
        VertexInfo vertexInfo, RenderPass pass)
    {
        _device = device;
        Effect = effect;
        var shaderStages = stackalloc PipelineShaderStageCreateInfo[effect.Stages.Length];
        for (var i = 0; i < effect.Stages.Length; i++) shaderStages[i] = effect.Stages[i].GetShaderStageCreateInfo();
        fixed (VertexInputAttributeDescription* pAttributeDescriptions = vertexInfo.AttributeDescriptions)
        fixed (VertexInputBindingDescription* pBindingDescriptions = vertexInfo.BindingDescriptions)
        {
            var vertexInput = new PipelineVertexInputStateCreateInfo
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexAttributeDescriptionCount = vertexInfo.AttributeDescriptions is not null
                    ? (uint) vertexInfo.AttributeDescriptions!.Length
                    : 0u,
                PVertexAttributeDescriptions = pAttributeDescriptions,
                VertexBindingDescriptionCount = vertexInfo.BindingDescriptions is not null
                    ? (uint) vertexInfo.BindingDescriptions!.Length
                    : 0u,
                PVertexBindingDescriptions = pBindingDescriptions
            };
            var colorBlendInfo = new PipelineColorBlendStateCreateInfo
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOpEnable = false,
                LogicOp = LogicOp.Copy,
                AttachmentCount = 1,
                PAttachments = &description.ColorBlendAttachment
            };
            var createInfo = new GraphicsPipelineCreateInfo
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = 2,
                PStages = shaderStages,
                PVertexInputState = &vertexInput,
                PInputAssemblyState = &description.InputAssemblyInfo,
                PViewportState = &description.ViewportInfo,
                PRasterizationState = &description.RasterizationInfo,
                PMultisampleState = &description.MultisampleInfo,
                PColorBlendState = &colorBlendInfo,
                PDepthStencilState = &description.DepthStencilInfo,
                PDynamicState = &description.DynamicStateInfo,
                Layout = effect.EffectLayout,
                RenderPass = pass,
                Subpass = description.SubPass,
                BasePipelineIndex = -1,
                BasePipelineHandle = default
            };
            vk.CreateGraphicsPipelines(device, default, 1, createInfo, null, out Pipeline).Validate(_device.Logger);
        }
    }

    public static implicit operator Pipeline(GraphicsPipeline p) => p.Pipeline;

    public void Dispose()
    {
        _device.WaitIdle();
        vk.DestroyPipeline(_device, Pipeline, null);
    }
}
