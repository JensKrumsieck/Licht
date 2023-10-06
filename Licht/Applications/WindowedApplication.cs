using System.Collections.ObjectModel;
using Licht.Applications.DependencyInjection;
using Licht.Graphics;

namespace Licht.Applications;

public class WindowedApplication : BaseApplication
{
    private readonly Window _window;
    private readonly IRenderer _renderer;
    
    public WindowedApplication(ReadOnlyDictionary<Type, ServiceDescriptor> services, ApplicationSpecification config) : base(services, config)
    {
        _window = new Window(Logger, config.ApplicationName, config.Width, config.Height, config.IsFullscreen);
        _window.Update += Update;
        _renderer = GetService<IRenderer>();
    }

    public override void Run()
    {
        base.Run();
        _window.Run();
    }
    
    public override void Update(float deltaTime)
    {
        //window related logic here
    }

    public override void Dispose()
    {
        base.Dispose();
        _window.Update -= Update;
        _window.Dispose();
        GC.SuppressFinalize(this);
    }
}
