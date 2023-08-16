using Catalyst.Engine.Graphics;
using Silk.NET.Windowing;

namespace Catalyst.Engine;

public class Application : IDisposable
{
    private readonly List<ILayer> _layerStack = new();
    private readonly IWindow _window;

    private readonly GraphicsDevice _device;
    private readonly Renderer _renderer;

    public Application()
    {
        _window = Window.Create(WindowOptions.DefaultVulkan);
        _window.Initialize();
        _device = new GraphicsDevice(_window);
        _renderer = new Renderer(_device, _window);
    }

    public void AttachLayer(ILayer layer)
    {
        _layerStack.Add(layer);
        layer.OnAttach();
    }
    
    public void Run()
    {
        _window.Render += DrawFrame;
        _window.Run();
        _window.Close();
    }

    private void DrawFrame(double deltaTime)
    {
        foreach (var layer in _layerStack)
        {
            layer.OnUpdate(deltaTime);
            layer.OnDrawGui(deltaTime);
        }
    }
    public void Dispose()
    {
        _renderer.Dispose();
        _device.Dispose();
        _window.Dispose();
        GC.SuppressFinalize(this);
    }
}