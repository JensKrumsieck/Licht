using Licht.GraphicsCore.Graphics;
using Microsoft.Extensions.Logging;

namespace Licht.Applications;

public class WindowedApplication : BaseApplication
{
    private readonly Window _window;
    private readonly IRenderer _renderer;

    public WindowedApplication(ILogger logger, IRenderer renderer, Window window) : base(logger)
    {
        _renderer = renderer;
        _window = window;
        
        _window.Update += Update;
        _window.Render += Render;
    }

    public override void Run()
    {
        base.Run();
        _window.Run();
    }

    public override void Render(float deltaTime)
    {
        base.Render(deltaTime);
        var cmd = _renderer.BeginFrame();
        _renderer.BeginRenderPass(cmd);
        DrawFrame(cmd, deltaTime);
        _renderer.EndRenderPass(cmd);
        _renderer.EndFrame();
    }

    public virtual void DrawFrame(ICommandList cmd, float deltaTime)
    {
        //does nothing!
    }

    public override void Dispose()
    {
        base.Dispose();
        
        _window.Update -= Update;
        _window.Render -= Render;
        _window.Dispose();
        GC.SuppressFinalize(this);
    }
}
