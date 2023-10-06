using Licht.Applications.DependencyInjection;
using Licht.Core;
using Licht.Graphics;

namespace Licht.Applications;

public class ApplicationBuilder
{
    public readonly ServiceCollection Services = new();
    public IApplication Build<T>(ApplicationSpecification specification) where T : BaseApplication
    {
        var container = Services.Build();
        if(!container.ContainsKey(typeof(ILogger))) throw new Exception("Can not create application with no logging service registered");
        if(!container.ContainsKey(typeof(IRenderer))) throw new Exception("Can not create application with no rendering service registered");
        return (T) Activator.CreateInstance(typeof(T), container, specification)!;
    }
}
