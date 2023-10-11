using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Licht.Applications;

public abstract class BaseApplication : IApplication
{
    protected readonly ILogger Logger;
    private bool _isDisposed = false;

    internal ServiceProvider Services = null!;
    
    protected BaseApplication(ILogger logger)
    {
        Logger = logger;
    }

    protected internal virtual void Initialize(ApplicationSpecification config) => Logger.LogTrace("Application created with {Specification}", config);

    public virtual void Run() => Logger.LogTrace("Application running");

    protected void Update(double deltaTime) => Update((float) deltaTime);
    protected void Render(double deltaTime) => Render((float) deltaTime);
    
    public virtual void Update(float deltaTime){ }
    public virtual void Render(float deltaTime){ }

    public virtual void Release() { }
    
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Logger.LogTrace("Application exit");
        Release();
        Services.Dispose();
        GC.SuppressFinalize(this);
    }
}
