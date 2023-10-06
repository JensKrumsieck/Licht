using Licht.Applications.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Licht.Applications;

public class ApplicationBuilder
{
    public readonly ServiceCollection Services = new();
    public IApplication Build<T>(ApplicationSpecification specification) where T : BaseApplication
    {
        var app = (T) Activator.CreateInstance(typeof(T))!;
        app.Services = Services.Build();
        app.Logger = app.GetService<ILogger>();
        app.Initialize(specification);
        app.Logger.LogTrace("Application created with {Specification}", specification);
        return app;
    }
}
