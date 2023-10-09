using Licht.Core.Graphics;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Contexts;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Licht.Applications;

public sealed class Window : IVkSurfaceSource, IGLContextSource, IWindowProvider, IDisposable
{
    private readonly IWindow _view;
    private readonly ILogger _logger;

    public uint Width { get; private set; }
    public uint Height { get; private set; }

    public event Action<double>? Update
    {
        add => _view.Update += value;
        remove => _view.Update -= value;
    }
    public event Action<double>? Render
    {
        add => _view.Render += value;
        remove => _view.Render -= value;
    }

    public Window(ILogger logger, string name, int width, int height, bool fullscreen)
    {
        _logger = logger;
        var opt = WindowOptions.DefaultVulkan with
        {
            Title = name,
            Size = new Vector2D<int>(width, height),
            WindowState = fullscreen ? WindowState.Fullscreen : WindowState.Normal
        };
        _view = Silk.NET.Windowing.Window.Create(opt);
        _logger.LogTrace("Window created");
        Width = (uint) width;
        Height = (uint) height;
        _view.Initialize();
        _logger.LogTrace("Window initialized");
        _view.Resize += OnResize;
    }
    
    private void OnResize(Vector2D<int> newSize)
    {
        Width = (uint) newSize.X;
        Height = (uint) newSize.Y;
        _logger.LogTrace("Window resized to {NewSize}", newSize);
    }

    public void Run()
    {
        _logger.LogTrace("Start Window Loop");
        while (!_view.IsClosing)
        {
            _view.DoEvents();
            if (!_view.IsClosing)
            {
                _view.DoUpdate();
                _view.DoRender();
            }
        }
        _view.DoRender();
        _view.Reset();
        _logger.LogTrace("End Window Loop");
    }
    
    public void Dispose() => _view.Dispose();

    public IVkSurface? VkSurface => _view.VkSurface;
    public IGLContext? GLContext => _view.GLContext;
    IWindow IWindowProvider.Window => _view;
}
