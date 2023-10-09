using Licht.Core.Graphics;
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
        _renderer.BeginFrame();
        
        _renderer.EndFrame();
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
