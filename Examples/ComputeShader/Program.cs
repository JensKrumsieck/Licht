using Licht.Applications;
using Licht.Core;
using Licht.Vulkan;
using Licht.Vulkan.Memory;
using Licht.Vulkan.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using CommandBuffer = Licht.Vulkan.CommandBuffer;
using DescriptorPool = Licht.Vulkan.DescriptorPool;
using DescriptorSetLayout = Licht.Vulkan.DescriptorSetLayout;

var opts = ApplicationSpecification.Default with {IsResizeable = false, Height = 500, Width = 500};
var builder = new ApplicationBuilder(opts);

builder.Services.AddSingleton<ILogger, Logger>();
builder.Services.AddWindow(opts);
builder.Services.AddVulkanRenderer<PassthroughAllocator>();

{
    using var app = builder.Build<RaytracingApplication>();
    app.Run();
}

sealed unsafe class RaytracingApplication : WindowedApplication
{
    private readonly VkGraphicsDevice _device;
    private readonly VkImage _image;
    private readonly CommandBuffer _computeCmd;
    private readonly PipelineEffect _computeEffect;
    private readonly PipelineEffect _graphicsEffect;
    private readonly VkComputePipeline _computePipeline;
    private readonly VkGraphicsPipeline _pipeline;
    private readonly DescriptorPool _descriptorPool;
    private readonly DescriptorSetLayout _descriptorSetLayout;
    private DescriptorSet _descriptorSet;
    
    public RaytracingApplication(ILogger logger, VkGraphicsDevice device, VkRenderer renderer, IWindow window) : base(logger, renderer, window)
    {
        _device = device;
        _computeCmd = _device.AllocateCommandBuffers(1)[0];
        
        var poolSizes = new DescriptorPoolSize[]
        {
            new() {Type = DescriptorType.StorageImage, DescriptorCount = 1000},
            new() {Type = DescriptorType.Sampler, DescriptorCount = 1000}
        };
        _descriptorPool =_device.CreateDescriptorPool(poolSizes, 10);
        var binding0 = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageImage,
            StageFlags = ShaderStageFlags.ComputeBit
        };
        var binding1 = new DescriptorSetLayoutBinding
        {
            Binding = 1,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            StageFlags = ShaderStageFlags.FragmentBit
        };
        _descriptorSetLayout = _device.CreateDescriptorSetLayout(new[]{binding0, binding1});
        _descriptorSet = _descriptorPool.AllocateDescriptorSet(_descriptorSetLayout);
        _computeEffect = PipelineEffect.BuildComputeEffect(_device, "./assets/shaders/raytracing_simple.comp.spv", new[]{_descriptorSetLayout});
        _computePipeline = new VkComputePipeline(_device, _computeEffect);
        
        _graphicsEffect = PipelineEffect.BuildEffect(_device, "./assets/shaders/quad.vert.spv", "./assets/shaders/quad.frag.spv", new[] {_descriptorSetLayout});
        _pipeline = new VkGraphicsPipeline(_device, _graphicsEffect, GraphicsPipelineDescription.Default(), new VertexInfo(), renderer.RenderPass!.Value);
        
        var width = renderer.Extent?.Width ?? 0;
        var height = renderer.Extent?.Height ?? 0;
        _image = new VkImage(_device, width, height, Format.B8G8R8A8Unorm, ImageLayout.General, ImageUsageFlags.TransferSrcBit | ImageUsageFlags.StorageBit | ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.SampledBit);
        _device.UpdateDescriptorSetImage(ref _descriptorSet, _image.ImageInfo, DescriptorType.StorageImage, 0);
        _device.UpdateDescriptorSetImage(ref _descriptorSet, _image.ImageInfo with {ImageLayout = ImageLayout.ShaderReadOnlyOptimal}, DescriptorType.CombinedImageSampler, 1);
    }
    
    public override void BeforeDraw()
    {
        base.BeforeDraw();
        _computeCmd.Begin();
        _image.TransitionLayout(_computeCmd, ImageLayout.General, 1, 1);
        _computeCmd.BindComputePipeline(_computePipeline);
        _computeCmd.BindComputeDescriptorSet(_descriptorSet, _computeEffect);
        _computeCmd.Dispatch(_image.Width/16 + 1,_image.Height/16 + 1,1);
        _image.TransitionLayout(_computeCmd, ImageLayout.ShaderReadOnlyOptimal, 1, 1);
        _computeCmd.End();
        _device.SubmitCommandBufferToQueue(_computeCmd, default);
        _device.WaitForQueue(); //could use a fence here ^^
    }

    public override void DrawFrame(CommandBuffer cmd, float deltaTime)
    {
        base.DrawFrame(cmd, deltaTime);
        cmd.BindGraphicsPipeline(_pipeline);
        cmd.BindGraphicsDescriptorSet(_descriptorSet, _graphicsEffect);
        cmd.Draw(4, 1, 0, 0);
    }

    public override void Release()
    {
        base.Release();
        _device.WaitIdle();
        _image.Dispose();
        _computePipeline.Dispose();
        _pipeline.Dispose();
        _computeEffect.Dispose();
        _graphicsEffect.Dispose();
        _descriptorPool.FreeDescriptorSet(_descriptorSet);
        _descriptorSetLayout.Dispose();
        _descriptorPool.Dispose();
    }
}