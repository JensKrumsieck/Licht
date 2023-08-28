using Catalyst.Tools;
using Silk.NET.Vulkan;

namespace Catalyst.Pipeline;

public unsafe struct ComputePass : IDisposable, IConvertibleTo<Silk.NET.Vulkan.Pipeline>
{
    private readonly Device _device;
    public readonly Silk.NET.Vulkan.Pipeline Pipeline;
    private readonly ShaderEffect _effect;
    public PipelineLayout Layout => _effect.EffectLayout;

    public ComputePass(Device device, ShaderEffect effect)
    {
        _device = device;
        _effect = effect;

        var createInfo = new ComputePipelineCreateInfo
        {
            SType = StructureType.ComputePipelineCreateInfo,
            Layout = Layout,
            Stage = effect.Stages[0].GetShaderStageCreateInfo(),
            Flags = 0
        };

        vk.CreateComputePipelines(device, default, 1, createInfo, null, out Pipeline).Validate();
    }
    public static implicit operator Silk.NET.Vulkan.Pipeline(ComputePass p) => p.Pipeline;
    public Silk.NET.Vulkan.Pipeline Convert() => Pipeline;
    public void Dispose() => vk.DestroyPipeline(_device, Pipeline, null);
}
