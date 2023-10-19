using Licht.Applications;
using Licht.Core;
using Licht.Vulkan;
using Licht.Vulkan.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silk.NET.Windowing;
using DefaultEcs;
using Licht.Rendering;
using Licht.Scene;
using Licht.Vulkan.Pipelines;

var opts = ApplicationSpecification.Default with {ApplicationName = "Triangle"};
var builder = new ApplicationBuilder(opts);

builder.Services.AddSingleton<ILogger, Logger>();
builder.Services.AddWindow(opts);
builder.Services.AddVulkanRenderer<PassthroughAllocator>();

{
    using var app = builder.Build<Engine>();
    app.Run();
}

class Engine : WindowedApplication
{
    private readonly World _scene = new();

    public Engine(ILogger logger, VkGraphicsDevice device, VkRenderer renderer, IWindow window) : base(logger, renderer, window)
    {
        var compiler = new PipelineCompiler(device);
        var vertStage = compiler.CompileShaderBytes("./assets/shaders/lit.vert");
        var fragStage = compiler.CompileShaderBytes("./assets/shaders/lit.frag");
        
        var suzanne = MeshImporter.FromFile("./assets/models/suzanne.fbx")[0];
        var e = _scene.CreateEntity(); 
        e.Set(new TransformComponent());
        e.Set(new MeshComponent(suzanne.Process(device)));
    }

    public override void DrawFrame(CommandBuffer cmd, float deltaTime)
    {
        base.DrawFrame(cmd, deltaTime);
        //could be AEntitySetSystem<T>
        var rendered = _scene.GetEntities().With(typeof(TransformComponent), typeof(MeshComponent)).AsSet();
        foreach (var e in rendered.GetEntities())
        {
            ref var t = ref e.Get<TransformComponent>();
            ref var m = ref e.Get<MeshComponent>();
            //push constant t
            m.Mesh.Bind(cmd);
            m.Mesh.Draw(cmd);
        }
    }

    public override void Release()
    {
        base.Release();
        //is there a better way? there must be!
        foreach (var e in _scene.GetAll<MeshComponent>())
        {
            e.Dispose();
        }
        _scene.Dispose();
    }
}