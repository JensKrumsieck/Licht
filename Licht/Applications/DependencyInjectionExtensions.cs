using Licht.Applications;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Contexts;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static void AddWindow(this ServiceCollection collection, ApplicationSpecification opts)
    {
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(opts.Width, opts.Height),
            WindowState = opts.IsFullscreen ? WindowState.Fullscreen : WindowState.Normal
        };
        var window = Window.Create(options);
        window.Initialize();
        collection.AddSingleton(window);
    }
}
