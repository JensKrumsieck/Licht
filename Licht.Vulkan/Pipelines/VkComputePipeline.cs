using Licht.Vulkan.Extensions;
using Silk.NET.Vulkan;

namespace Licht.Vulkan.Pipelines;

public unsafe class VkComputePipeline : IDisposable
{
    private readonly VkGraphicsDevice _device;
    private readonly PipelineEffect _effect;
    private readonly Pipeline _pipeline;
    private readonly DescriptorPool _pool;
    public VkComputePipeline(VkGraphicsDevice device, PipelineEffect effect)
    {
        _device = device;
        _effect = effect;
        var poolSizes = new DescriptorPoolSize[] {new() {Type = DescriptorType.StorageImage, DescriptorCount = 1000}};
        _pool = _device.CreateDescriptorPool(poolSizes);
        var computeInfo = new ComputePipelineCreateInfo
        {
            SType = StructureType.ComputePipelineCreateInfo,
            Layout = _effect.EffectLayout,
            Stage = _effect.Stages[0].GetShaderStageCreateInfo()
        };
        vk.CreateComputePipelines(_device, default, 1, computeInfo, null, out _pipeline).Validate(_device.Logger);
    }
    
    public void Dispose() => vk.DestroyPipeline(_device, _pipeline, null);
}
