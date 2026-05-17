using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using CoreOne.Hubs;

namespace CoreOne.Extensions;

public static class ServiceCollectionExtensions
{
    private record DefineService(Type Abstract, Type Concrete, ServiceLifetime Lifetime);

    /// <summary>
    /// Register core services to DI service container
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddScoped<IHub, Hub>()
            .AddSingleton<JsonSerializerSettings>(new NewtonSettings())
            .AddKeyedSingleton<ISerializer, NJsonService>("json");
        return services;
    }

    public static IServiceCollection RegisterTypesfromAssembly<T>(this IServiceCollection services)
    {
        var assembly = typeof(T).Assembly;

        Type[] allTypes;
        try
        {
            allTypes = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            allTypes = Array.FindAll(ex.Types, t => t != null)!;
        }

        var definitions = new Data<Type, DefineService>();
        foreach (var serviceType in allTypes)
        {
            var lifetimeDefinition = serviceType.GetCustomAttribute<ServiceRegistrationAttribute>();

            Type? proxyType = null;
            var proxyName = serviceType.Name + "Proxy";
            proxyType = allTypes.FirstOrDefault(t => t.Name.Matches(proxyName) && t.Namespace == serviceType.Namespace && t.IsSubclassOf(serviceType));
            if (serviceType.IsClass)
            {
                var lifetime = lifetimeDefinition?.Lifetime ?? ServiceLifetime.Scoped;
                var concrete = proxyType ?? serviceType;
                definitions.Set(serviceType, new DefineService(serviceType, concrete, lifetime));
                foreach (var iface in serviceType.GetInterfaces())
                    definitions.Set(iface, new DefineService(iface, concrete, lifetime));
            }

            var attributes = serviceType.GetCustomAttributes<InterceptedByAttribute>(false);
            if (attributes.Any())
            {
                foreach (var attr in attributes)
                {
                    definitions.Set(attr.InterceptorType, new DefineService(attr.InterceptorType, attr.InterceptorType, attr.Lifetime));
                }
            }
        }

        foreach (var (type, concrete, lifetime) in definitions.Values)
        {
            _ = lifetime switch {
                ServiceLifetime.Singleton => services.AddSingleton(type, concrete),
                ServiceLifetime.Scoped => services.AddScoped(type, concrete),
                ServiceLifetime.Transient => services.AddTransient(type, concrete),
                _ => throw new InvalidOperationException(nameof(lifetime))
            };
        }

        return services;
    }

    /// <summary>
    /// Removes the first registered service from DI container
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection Remove<T>(this IServiceCollection services)
    {
        var service = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(T));
        if (service is not null)
            services.Remove(service);

        return services;
    }
}