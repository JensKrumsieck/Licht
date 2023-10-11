using Licht.Applications;
using Licht.Core;
using Licht.Vulkan;
using Licht.Vulkan.Memory;
using Licht.Vulkan.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var opts = ApplicationSpecification.Default;
var builder = new ApplicationBuilder(opts);

builder.Services.AddSingleton<ILogger, Logger>();
builder.Services.AddWindow(opts);
builder.Services.AddVulkanRenderer();
//use the simple allocator (the only one yet!)
builder.Services.AddSingleton<IAllocator, PassthroughAllocator>();

using var app = builder.Build<TriangleApplication>();

app.Run();

class TriangleApplication : WindowedApplication
{
    private readonly VkGraphicsDevice _device;
    private readonly VkGraphicsPipeline _pipeline;
    private readonly ShaderEffect _effect;
    public TriangleApplication(ILogger logger, VkGraphicsDevice device, Window window, VkRenderer renderer) : base(logger, renderer, window)
    {
        _device = device;
        var passDescription = ShaderPassDescription.Default();
        _effect = ShaderEffect.BuildEffect(_device, "./assets/shaders/triangle.vert.spv", "./assets/shaders/triangle.frag.spv", null);
        _pipeline = new VkGraphicsPipeline(_device, _effect, passDescription, default, Renderer.RenderPass!.Value);
    }

    public override void DrawFrame(VkCommandBuffer cmd, float deltaTime)
    {
        cmd.BindGraphicsPipeline(_pipeline);
        cmd.Draw(3, 1, 0, 0);
    }

    public override void Dispose()
    {
        base.Dispose();
        _effect.Dispose();
        _pipeline.Dispose();
    }
}