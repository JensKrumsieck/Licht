using System.Collections.ObjectModel;
using Silk.NET.Core.Contexts;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Catalyst.Applications;

public class ServiceCollection
{
    private readonly Dictionary<Type, ServiceDescriptor> _serviceTypes = new();
    public void RegisterSingleton<TService>() => _serviceTypes.Add(typeof(TService), new ServiceDescriptor(typeof(TService), Lifetime.Singleton));
    public void RegisterSingleton<TService>(TService implementation) where TService : class => _serviceTypes.Add(typeof(TService), new ServiceDescriptor(typeof(TService), implementation, Lifetime.Singleton));
    public void RegisterSingleton<TService, TImplementation>() where TImplementation : TService => _serviceTypes.Add(typeof(TService), new ServiceDescriptor(typeof(TService), typeof(TImplementation), Lifetime.Singleton));
    public void RegisterTransient<TService>() => _serviceTypes.Add(typeof(TService), new ServiceDescriptor(typeof(TService), Lifetime.Transient));
    public void RegisterTransient<TService, TImplementation>() where TImplementation : TService => _serviceTypes.Add(typeof(TService), new ServiceDescriptor(typeof(TService), typeof(TImplementation), Lifetime.Transient));

    public bool HasService<TService>() => _serviceTypes.ContainsKey(typeof(TService));
    
    public IWindow AddWindowing(WindowOptions options)
    {
        var window = Window.Create(options);
        window.Initialize();
        RegisterSingleton(window);
        return window;
    }
    public void AddInput(IWindow window)
    {
        var input = window.CreateInput();
        RegisterSingleton(input);
    }
    
    public ReadOnlyDictionary<Type, ServiceDescriptor> Build() => new(_serviceTypes);
}
