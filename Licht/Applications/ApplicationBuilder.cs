using Microsoft.Extensions.DependencyInjection;

namespace Licht.Applications;

public class ApplicationBuilder
{
    private readonly ApplicationSpecification _opts;
    public ApplicationBuilder(ApplicationSpecification specification) => _opts = specification;
    
    public readonly ServiceCollection Services = new();
    
    public IApplication Build<T>() where T : BaseApplication
    {
        Services.AddSingleton<BaseApplication, T>();
        var serviceProvider = Services.BuildServiceProvider(new ServiceProviderOptions{ValidateOnBuild = true});
        var app = serviceProvider.GetService<BaseApplication>()!;
        app.Services = serviceProvider;
        app.Initialize(_opts);
        return app;
    }
}
