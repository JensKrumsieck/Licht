using System.Collections.ObjectModel;

namespace Licht.Applications.DependencyInjection;

public class DiContainer
{
    internal ReadOnlyDictionary<Type, ServiceDescriptor> Services;
    private object GetService(Type serviceType)
    {
        if (!Services.TryGetValue(serviceType, out var type))
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
}
