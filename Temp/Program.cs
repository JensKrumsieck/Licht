using Licht.Applications;
using Licht.Core;
using Licht.GraphicsCore.Graphics;
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
    private VkGraphicsDevice _device;
    private GraphicsPipeline _pipeline;
    private ShaderEffect _effect;
    public TriangleApplication(ILogger logger, IRenderer renderer, Window window, VkGraphicsDevice device) : base(logger, renderer, window)
    {
        if (renderer is not VkRenderer vkRenderer) throw new Exception("Use Vulkan, biatch!");
        _device = device;
        var passDescription = ShaderPassDescription.Default();
        _effect = ShaderEffect.BuildEffect(device, "./assets/shaders/triangle.vert.spv", "./assets/shaders/triangle.frag.spv", null);
        _pipeline = new GraphicsPipeline(device, _effect, passDescription, default, vkRenderer.RenderPass!.Value);
    }

    public override void DrawFrame(ICommandList cmd, float deltaTime)
    {
        if (cmd is not VkCommandBuffer vkCmd) throw new Exception("Use Vulkan, biatch");
        vkCmd.BindGraphicsPipeline(_pipeline);
        vkCmd.Draw(3, 1, 0, 0);
    }

    public override void Dispose()
    {
        base.Dispose();
        _effect.Dispose();
        _pipeline.Dispose();
    }
}