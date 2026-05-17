using Microsoft.Extensions.DependencyInjection;

namespace CoreOne.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped, Type? servicingType = null) : Attribute
{
    public ServiceLifetime Lifetime { get; } = lifetime;
    public Type? ServicingType { get; } = servicingType;
}

public class ServiceAttribute<TInterface>(ServiceLifetime lifetime = ServiceLifetime.Scoped) : ServiceAttribute(lifetime, typeof(TInterface)) where TInterface : class
{
}