using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using System.Diagnostics;

namespace CoreOne.Extensions;

public static class ServiceCollectionExtensions
{
    private record DefineService(Type Abstract, Type Concrete, ServiceLifetime Lifetime, bool ForceSet);

    /// <summary>
    /// Register core services to DI service container
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services
            .AddSingleton<JsonSerializerSettings>(new NewtonSettings())
            .AddKeyedSingleton<ISerializer, NJsonService>("json");
        services.AddTypesFromAssembly<IClock>();
        return services;
    }

    public static IServiceCollection AddTypesFromAssembly<T>(this IServiceCollection services)
    {
        var assembly = typeof(T).Assembly;

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = Array.FindAll(ex.Types, t => t != null)!;
        }

        var definitions = new Data<Type, DefineService>();
        var query = from p in types
                    where p.IsPublic && p.IsClass && !p.IsInterface && !p.IsAbstract
                    let attribute = p.GetCustomAttribute<ServiceAttribute>(true)
                    where attribute != null
                    select (p, attribute);
        var names = query.Select(p => p.p.FullName)
            .ExcludeNullOrEmpty()
            .OrderBy(p => p, MStringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (names.Any(p => p.Contains("NotificationService", StringComparison.OrdinalIgnoreCase)))
        { }

        var skipInterfaces = new HashSet<Type> {
            typeof(IDisposable),
            typeof(IAsyncDisposable)
        };
        foreach (var (service, attribute) in query)
        {
            Type? proxyType = null;
            var proxyName = service.Name + "Proxy";
            proxyType = types.FirstOrDefault(t => t.Name.Matches(proxyName) && t.Namespace == service.Namespace && t.IsSubclassOf(service));

            var lifetime = attribute?.Lifetime ?? ServiceLifetime.Scoped;
            var servicing = attribute?.ServicingType ?? service;
            var concrete = proxyType ?? service;
            definitions.SafeAdd(servicing, new DefineService(servicing, concrete, lifetime, attribute?.ServicingType is not null));
            foreach (var iface in service.GetInterfaces())
            {
                if (!skipInterfaces.Contains(iface))
                {
                    definitions.SafeAdd(iface, new DefineService(iface, concrete, lifetime, false));
                }
            }

            var attributes = service.GetCustomAttributes<InterceptAttribute>(false);
            if (attributes.Any())
            {
                foreach (var attr in attributes)
                {
                    definitions.SafeAdd(attr.InterceptorType, new DefineService(attr.InterceptorType, attr.InterceptorType, attr.Lifetime, false));
                }
            }
        }

        var registeredConcretes = new HashSet<Type>();
        foreach (var def in definitions.Values)
        {
            if (registeredConcretes.Add(def.Concrete))
            {
                if (def.Lifetime == ServiceLifetime.Singleton)
                    services.TryAddSingleton(def.Concrete, def.Concrete);
                else if (def.Lifetime == ServiceLifetime.Scoped)
                    services.TryAddScoped(def.Concrete, def.Concrete);
                else if (def.Lifetime == ServiceLifetime.Transient)
                    services.TryAddTransient(def.Concrete, def.Concrete);
            }
        }

        foreach (var (type, concrete, lifetime, forceSet) in definitions.Values)
        {
            if (type == concrete)
                continue;

            if (forceSet)
            {
                var registration = services.FirstOrDefault(descriptor => descriptor.ServiceType == type);
                if (registration is not null)
                    services.Remove(registration);
            }
            if (lifetime == ServiceLifetime.Singleton)
                services.TryAddSingleton(type, sp => sp.GetRequiredService(concrete));
            else if (lifetime == ServiceLifetime.Scoped)
                services.TryAddScoped(type, sp => sp.GetRequiredService(concrete));
            else if (lifetime == ServiceLifetime.Transient)
                services.TryAddTransient(type, sp => sp.GetRequiredService(concrete));
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