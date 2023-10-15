using Licht.Vulkan;
using Microsoft.Extensions.Logging;
using Silk.NET.Windowing;

namespace Licht.Applications;

public class WindowedApplication : BaseApplication
{
    protected readonly IWindow Window;
    protected readonly VkRenderer Renderer;
    private readonly ILogger _logger;

    public WindowedApplication(ILogger logger, VkRenderer renderer, IWindow window) : base(logger)
    {
        Renderer = renderer;
        Window = window;
        _logger = logger;
        
        Window.Update += Update;
        Window.Render += Render;
    }

    public override void Run()
    {
        base.Run();
        Window.Run();
    }

    public override void Render(float deltaTime)
    {
        base.Render(deltaTime);
        BeforeDraw();
        var cmd = Renderer.BeginFrame();
        Renderer.BeginRenderPass(cmd);
        DrawFrame(cmd, deltaTime);
        Renderer.EndRenderPass(cmd);
        Renderer.EndFrame();
        AfterDraw();
    }

    public virtual void BeforeDraw()
    {
        //does nothing!
    }
    
    public virtual void DrawFrame(CommandBuffer cmd, float deltaTime)
    {
        //does nothing!
    }
    
    public virtual void AfterDraw()
    {
        //does nothing!
    }

    public override void Release()
    { 
        base.Release();
        Window.Update -= Update;
        Window.Render -= Render;
    }
}
