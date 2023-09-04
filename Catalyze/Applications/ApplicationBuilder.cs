using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Catalyze.Applications;

public class ApplicationBuilder
{
    public readonly ServiceCollection Services = new();
    
    internal ApplicationBuilder() { }
    
    public Application Build()
    {
        return new Application(Services.Build(), Services.HasService<IWindow>(), Services.HasService<IInputContext>());
    }
}
