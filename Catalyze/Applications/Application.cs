using System.Collections.ObjectModel;
using Catalyze.Allocation;
using Catalyze.UI;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Catalyze.Applications;

public class Application : IDisposable
{    
    //app singleton
    private static Application? _application;
    public static ApplicationBuilder CreateBuilder() => new();
    
    private readonly ReadOnlyDictionary<Type, ServiceDescriptor> _serviceTypes;
    private readonly Dictionary<Type, IAppModule> _modules = new();
    private readonly List<IAppLayer> _layerStack = new();
    
    private GraphicsDevice? _device;
    private Renderer? _renderer;
    private ImGuiRenderer? _guiRenderer;
    private readonly IWindow? _window;
    private readonly IInputContext? _input;
    
    private readonly bool _useWindowing;
    private readonly bool _useInput;

    internal Application(ReadOnlyDictionary<Type, ServiceDescriptor> serviceTypes, bool useWindowing, bool useInput)
    {
        _application?.Dispose();
        _application = this;
        
        _serviceTypes = serviceTypes;
        _useWindowing = useWindowing;
        _useInput = useInput;
        if (_useWindowing) _window = GetService<IWindow>();
        if (_useInput) _input = GetService<IInputContext>();
    }

    private object GetService(Type serviceType)
    {
        if (!_serviceTypes.TryGetValue(serviceType, out var type))
            throw new Exception($"Service of Type { serviceType.Name} could not be found!");
        
        if (type.Implementation != null) return type.Implementation;
        var actualType = type.ImplementationType ?? type.ServiceType;

        if (actualType.IsInterface || actualType.IsAbstract)
            throw new Exception($"Can not instantiate abstract class or interface {actualType.Name}!");

        //TODO: select for highest overlap with registered services
        var ctor = actualType.GetConstructors().First();
        var parameters = ctor.GetParameters().Select(x => GetService(x.ParameterType)).ToArray();
        
        var service = Activator.CreateInstance(actualType, parameters)!;
        if(type.Lifetime == Lifetime.Singleton)
            type.Implementation = service;
        return service;
    }

    public T GetService<T>() where T : class => (T) GetService(typeof(T));

    public T? GetModule<T>() where T : IAppModule
    {
        if (_modules.TryGetValue(typeof(T), out var module)) return (T) module;
        return default;
    }

    public static Application GetInstance() => _application!;

    public Application UseVulkan(GraphicsDeviceCreateOptions options)
    {
        if (!_useWindowing) throw new Exception("Can not use Vulkan without enabling windowing!");
        _device = new GraphicsDevice(options, _window!, GetService<IAllocator>());
        _modules[typeof(Renderer)] = new Renderer(_device, _window!);
        return this;
    }
    
    public Application UseImGui()
    {
        if (!_modules.ContainsKey(typeof(Renderer))) throw new Exception("Vulkan is not initialized, can not use ImGUI");
        if (!_useInput) throw new Exception("ImGui needs IInputContext to work!");
        var renderer = GetModule<Renderer>()!;
        _modules[typeof(ImGuiRenderer)] = new ImGuiRenderer(renderer, _input!);
        return this;
    }
    
    private void AttachLayer(IAppLayer appLayer)
    {
        _layerStack.Add(appLayer);
        appLayer.OnAttach();
    }

    public void AttachLayer<T>() where T : IAppLayer, new() => AttachLayer(new T());

    public void Run()
    {
        //TODO: What should run look like if windowing is not used?
        if (!_useWindowing) return;
        
        _renderer = GetModule<Renderer>();
        _guiRenderer = GetModule<ImGuiRenderer>();
        
        _window!.Render += DrawFrame;
        _window.Run();
        _device?.WaitIdle();
        _window.Close();
    }
    private void DrawFrame(double deltaTime)
    {
        if (_renderer is null) return;
        
        var cmd = _renderer.BeginFrame();
        _renderer.BeginRenderPass(cmd);
        
        //update layers
        foreach (var layer in _layerStack) layer.OnUpdate(deltaTime);
        _guiRenderer?.OnUpdate(deltaTime);
        _guiRenderer?.Begin();
        foreach (var layer in _layerStack)  layer.OnDrawGui(deltaTime);
        _guiRenderer?.End();
        
        _renderer.EndRenderPass(cmd);
        _renderer.EndFrame();
    }

    public void Dispose()
    {
        foreach (var layer in _layerStack) layer.OnDetach();
        foreach (var implementation in _serviceTypes.Select(x => x.Value.Implementation).Where(i => i is not null))
        {
            if(implementation is IDisposable disposable)
                disposable.Dispose();
        }
        _guiRenderer?.Dispose();
        _renderer?.Dispose();
        _device?.Dispose();
        GC.SuppressFinalize(this);
    }
}
