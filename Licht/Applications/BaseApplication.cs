using Licht.Applications.DependencyInjection;
using Licht.Core;
using Microsoft.Extensions.Logging;

namespace Licht.Applications;

public abstract class BaseApplication : DiContainer, IApplication
{
    protected internal ILogger Logger = null!;

    protected internal virtual void Initialize(ApplicationSpecification config) => Logger = GetService<ILogger>();

    public virtual void Run() => Logger.LogTrace("Application running");

    protected void Update(double deltaTime) => Update((float) deltaTime);
    protected void Render(double deltaTime) => Render((float) deltaTime);
    
    public virtual void Update(float deltaTime){ }
    public virtual void Render(float deltaTime){ }

    public virtual void Dispose()
    {
        Logger.LogTrace("Application exit");
        GC.SuppressFinalize(this);
    }
}
