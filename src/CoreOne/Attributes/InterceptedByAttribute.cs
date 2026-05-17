using Microsoft.Extensions.DependencyInjection;

namespace CoreOne.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InterceptedByAttribute(Type interceptorType, ServiceLifetime lifetime = ServiceLifetime.Scoped) : Attribute
{
    public Type InterceptorType { get; } = interceptorType;
    public ServiceLifetime Lifetime { get; } = lifetime;
}

public class InterceptedByAttribute<TInterceptor>(ServiceLifetime lifetime = ServiceLifetime.Scoped) : InterceptedByAttribute(typeof(TInterceptor), lifetime) where TInterceptor : IAsyncInterceptor
{ }