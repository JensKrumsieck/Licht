namespace Catalyze.Applications;

public class ServiceDescriptor
{
    public Type ServiceType { get; }
    public Type? ImplementationType { get; }
    public object? Implementation { get; internal set; }
    public Lifetime Lifetime { get; }
    
    public ServiceDescriptor(Type serviceType, object implementation, Lifetime lifetime)
    {
        ServiceType = serviceType;
        Implementation = implementation;
        Lifetime = lifetime;
        ImplementationType = implementation.GetType();
    }
    
    public ServiceDescriptor(Type serviceType, Lifetime lifetime)
    {
        ServiceType = serviceType;
        Lifetime = lifetime;
    }
    
    public ServiceDescriptor(Type serviceType, Type implementationType, Lifetime lifetime)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
        Lifetime = lifetime;
    }
}
