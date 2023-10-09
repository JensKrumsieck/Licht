using Licht.Core.Graphics;

namespace Licht.Applications;

public class WindowedApplication : BaseApplication
{
    private Window _window = null!;
    private readonly IRenderer _renderer;

    public WindowedApplication(IRenderer renderer)
    {
        _renderer = renderer;
    }
    
    protected internal override void Initialize(ApplicationSpecification config)
    {
        base.Initialize(config); 
        
        _window = new Window(Logger, config.ApplicationName, config.Width, config.Height, config.IsFullscreen);
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
        _window.Dispose();
        GC.SuppressFinalize(this);
    }
}
