﻿using Licht.Applications;
using Licht.Core;
using Licht.Vulkan;
using Licht.Vulkan.Memory;
using Licht.Vulkan.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silk.NET.Windowing;

var opts = ApplicationSpecification.Default with {ApplicationName = "Triangle"};
var builder = new ApplicationBuilder(opts);

builder.Services.AddSingleton<ILogger, Logger>();
builder.Services.AddWindow(opts);
builder.Services.AddVulkanRenderer<PassthroughAllocator>();

{
    using var app = builder.Build<TriangleApplication>();
    app.Run();
}

sealed class TriangleApplication : WindowedApplication
{
    private readonly VkGraphicsDevice _device;
    private readonly VkGraphicsPipeline _pipeline;
    private readonly PipelineEffect _effect;
    public TriangleApplication(ILogger logger, VkGraphicsDevice device, IWindow window, VkRenderer renderer) : base(logger, renderer, window)
    {
        _device = device;
        var passDescription = GraphicsPipelineDescription.Default();
        _effect = PipelineEffect.BuildEffect(_device, "./assets/shaders/triangle.vert.spv", "./assets/shaders/triangle.frag.spv", null);
        _pipeline = new VkGraphicsPipeline(_device, _effect, passDescription, default, Renderer.RenderPass!.Value);
    }

    public override void DrawFrame(CommandBuffer cmd, float deltaTime)
    {
        cmd.BindGraphicsPipeline(_pipeline);
        cmd.Draw(3, 1, 0, 0);
    }

    public override void Release()
    {
        _device.WaitIdle();
        _pipeline.Dispose();        
        _effect.Dispose();
        base.Release();
    }
}
