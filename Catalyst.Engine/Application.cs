using Catalyst.Engine.Graphics;
using Catalyst.Engine.UI;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Catalyst.Engine;

public class Application : IDisposable
{
    private static Application? _application;
    
    private readonly List<ILayer> _layerStack = new();
    private readonly IWindow _window;
    private readonly IInputContext _input;
    private readonly GraphicsDevice _device;
    private readonly Renderer _renderer;
    private readonly ImGuiLayer _uiLayer;

    public Application()
    {
        //set application
        _application?.Dispose();
        _application = this;
        
        _window = Window.Create(WindowOptions.DefaultVulkan);
        _window.Initialize();
        _device = new GraphicsDevice(_window);
        _renderer = new Renderer(_device, _window);
        _input = _window.CreateInput();
        
        _uiLayer = new ImGuiLayer();
        AttachLayer(_uiLayer);
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
        _device.WaitIdle();
        _window.Close();
    }

    private void DrawFrame(double deltaTime)
    {
        var cmd = _renderer.BeginFrame();
        _renderer.BeginRenderPass(cmd);
        foreach (var layer in _layerStack) layer.OnUpdate(deltaTime);
        
        _uiLayer.Begin();
        foreach (var layer in _layerStack)
        {
            layer.OnDrawGui(deltaTime);
        }
        _uiLayer.End();
        
        _renderer.EndRenderPass(cmd);
        _renderer.EndFrame();
    }

    public static Application GetApplication() => _application!;
    public static GraphicsDevice GetDevice() => _application!._device;
    public static Renderer GetRenderer() => _application!._renderer;
    public static IInputContext GetInput() => _application!._input;
    
    public void Dispose()
    {
        foreach (var layer in _layerStack) layer.OnDetach();
        _layerStack.Clear();
        
        _renderer.Dispose();
        _device.Dispose();
        _window.Dispose();
        GC.SuppressFinalize(this);
    }
}