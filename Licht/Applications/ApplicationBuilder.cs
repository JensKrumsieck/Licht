using Microsoft.Extensions.DependencyInjection;

namespace Licht.Applications;

public class ApplicationBuilder
{
    public readonly ServiceCollection Services = new();
    public IApplication Build<T>(ApplicationSpecification specification) where T : BaseApplication
    {
        Services.AddSingleton<BaseApplication, T>();
        var serviceProvider = Services.BuildServiceProvider(new ServiceProviderOptions());
        var app = serviceProvider.GetService<BaseApplication>()!;
        app.Services = serviceProvider;
        app.Initialize(specification);
        return app;
    }
}
