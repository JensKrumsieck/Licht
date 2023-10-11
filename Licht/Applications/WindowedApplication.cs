using Licht.Vulkan;
using Microsoft.Extensions.Logging;

namespace Licht.Applications;

public class WindowedApplication : BaseApplication
{
    protected readonly Window Window;
    protected readonly VkRenderer Renderer;

    public WindowedApplication(ILogger logger, VkRenderer renderer, Window window) : base(logger)
    {
        Renderer = renderer;
        Window = window;
        
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
        var cmd = Renderer.BeginFrame();
        Renderer.BeginRenderPass(cmd);
        DrawFrame(cmd, deltaTime);
        Renderer.EndRenderPass(cmd);
        Renderer.EndFrame();
    }

    public virtual void DrawFrame(CommandBuffer cmd, float deltaTime)
    {
        //does nothing!
    }

    public override void Release()
    { 
        base.Release();
        Renderer.Device.WaitIdle();
        Window.Update -= Update;
        Window.Render -= Render;
        Window.Dispose();
    }
}
