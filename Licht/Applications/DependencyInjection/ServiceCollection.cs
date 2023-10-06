using System.Collections.ObjectModel;
using Silk.NET.Windowing;

namespace Licht.Applications.DependencyInjection;

public class ServiceCollection
{
    private readonly Dictionary<Type, ServiceDescriptor> _serviceTypes = new();
    public void RegisterSingleton<TService>() => _serviceTypes.Add(typeof(TService), new ServiceDescriptor(typeof(TService), Lifetime.Singleton));
    public void RegisterSingleton<TService>(TService implementation) where TService : class => _serviceTypes.Add(typeof(TService), new ServiceDescriptor(typeof(TService), implementation, Lifetime.Singleton));
    public void RegisterSingleton<TService, TImplementation>() where TImplementation : TService => _serviceTypes.Add(typeof(TService), new ServiceDescriptor(typeof(TService), typeof(TImplementation), Lifetime.Singleton));
    public void RegisterTransient<TService>() => _serviceTypes.Add(typeof(TService), new ServiceDescriptor(typeof(TService), Lifetime.Transient));
    public void RegisterTransient<TService, TImplementation>() where TImplementation : TService => _serviceTypes.Add(typeof(TService), new ServiceDescriptor(typeof(TService), typeof(TImplementation), Lifetime.Transient));

    public bool HasService<TService>() => _serviceTypes.ContainsKey(typeof(TService));
    
    
    public ReadOnlyDictionary<Type, ServiceDescriptor> Build() => _serviceTypes.AsReadOnly();
}
