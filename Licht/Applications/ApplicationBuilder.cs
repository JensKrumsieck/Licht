using Licht.Applications.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Licht.Applications;

public class ApplicationBuilder
{
    public readonly ServiceCollection Services = new();
    public IApplication Build<T>(ApplicationSpecification specification) where T : BaseApplication
    {
        Services.RegisterSingleton<BaseApplication, T>();
        var serviceCollection = Services.Build();
        var container = new DiContainer { Services = serviceCollection };
        var app = container.GetService<BaseApplication>();
        app.Services = serviceCollection;
        app.Logger = app.GetService<ILogger>();
        app.Initialize(specification);
        app.Logger.LogTrace("Application created with {Specification}", specification);
        return app;
    }
}
