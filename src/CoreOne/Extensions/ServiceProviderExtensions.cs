using Microsoft.Extensions.DependencyInjection;

namespace CoreOne.Extensions;

public static class ServiceProviderExtensions
{
    public static object Resolve(this IServiceProvider services, Type type)
    {
        object? instance;
        try
        {
            instance = services.GetService(type);
            if (instance is null && !type.IsInterface)
            {
                var creator = services.GetService<TargetCreator>() ?? new TargetCreator(services);
                instance = creator.CreateInstance(type);
            }
        }
        finally { }
        return instance!;
    }

    public static TService Resolve<TService>(this IServiceProvider services) where TService : notnull => (TService)services.Resolve(typeof(TService));
}