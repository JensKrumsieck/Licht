using Licht.Applications;
using Licht.GraphicsCore;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Contexts;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static void AddWindow(this ServiceCollection collection, ApplicationSpecification opts)
    {
        collection.AddSingleton<Window>(l => new Window(l.GetService<ILogger>()!, opts.ApplicationName, opts.Width, opts.Height, opts.IsFullscreen));
        collection.AddSingleton<IWindowProvider>(p => p.GetService<Window>()!);
        collection.AddSingleton<IVkSurfaceSource>(p => p.GetService<Window>()!);
        collection.AddSingleton<IGLContextSource>(p => p.GetService<Window>()!);
    }
}
