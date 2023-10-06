using System.Collections.ObjectModel;
using Licht.Applications.DependencyInjection;
using Licht.Core;

namespace Licht.Applications;

public abstract class BaseApplication : IApplication
{
    protected readonly ILogger Logger;
    private readonly ReadOnlyDictionary<Type, ServiceDescriptor> _services;
    
    protected BaseApplication(ReadOnlyDictionary<Type, ServiceDescriptor> services, ApplicationSpecification config)
    {
        _services = services;
        if (!_services.ContainsKey(typeof(ILogger)))
            throw new Exception("Application needs a logging service");
        Logger = GetService<ILogger>();
        Logger.Trace($"Application created with {config}"); 
    }

    public virtual void Run()
    {
        Logger.Trace("Application running");
    }

    protected void Update(double deltaTime) => Update((float) deltaTime);
    
    public abstract void Update(float deltaTime);
    
    private object GetService(Type serviceType)
    {
        if (!_services.TryGetValue(serviceType, out var type))
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

    public virtual void Dispose()
    {
        Logger.Trace("Application exit");
        GC.SuppressFinalize(this);
    }
}
