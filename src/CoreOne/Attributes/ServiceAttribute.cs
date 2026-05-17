using Microsoft.Extensions.DependencyInjection;

namespace CoreOne.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped) : Attribute
{
    public ServiceLifetime Lifetime { get; } = lifetime;
}