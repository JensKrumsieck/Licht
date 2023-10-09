using Licht.Applications;
using Licht.Core;
using Licht.GraphicsCore.Graphics;
using Licht.Vulkan.Memory;
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
    public TriangleApplication(ILogger logger, IRenderer renderer, Window window) : base(logger, renderer, window) { }

    public override void DrawFrame(ICommandList cmd, float deltaTime)
    {
        base.DrawFrame(cmd, deltaTime);
    }
}