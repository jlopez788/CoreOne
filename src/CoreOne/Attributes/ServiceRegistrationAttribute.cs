using Microsoft.Extensions.DependencyInjection;

namespace CoreOne.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ServiceRegistrationAttribute(ServiceLifetime lifetime) : Attribute
{
    public ServiceLifetime Lifetime { get; } = lifetime;
}