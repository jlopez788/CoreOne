using Microsoft.Extensions.DependencyInjection;

namespace CoreOne.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class InterceptAttribute(Type interceptorType, ServiceLifetime lifetime = ServiceLifetime.Scoped) : Attribute
{
    public Type InterceptorType { get; } = interceptorType;
    public ServiceLifetime Lifetime { get; } = lifetime;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class InterceptAttribute<TInterceptor>(ServiceLifetime lifetime = ServiceLifetime.Scoped) : InterceptAttribute(typeof(TInterceptor), lifetime) where TInterceptor : IAsyncInterceptor
{ }