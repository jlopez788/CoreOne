using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoreOne.Http.Extensions;

public static class HttpServiceCollectionExtensions
{
    public static IHttpClientBuilder AddRestClient<TInterface>(
        this IServiceCollection services,
        Action<HttpClient>? configureClient = null,
        string? serializerKey = null)
        where TInterface : class
    {
        return services.AddRestClient<TInterface>(
            configureClient is null ? null : (_, client) => configureClient(client),
            serializerKey);
    }

    public static IHttpClientBuilder AddRestClient<TInterface>(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient>? configureClient,
        string? serializerKey = null)
        where TInterface : class
    {
        var interfaceType = typeof(TInterface);
        if (!interfaceType.IsInterface)
            throw new ArgumentException($"{interfaceType.Name} must be an interface.", nameof(TInterface));

        var (clientType, proxyType) = ResolveGeneratedClientTypes(interfaceType);
        var effectiveConcreteType = proxyType ?? clientType
            ?? throw new InvalidOperationException($"Could not find generated REST client implementation for interface '{interfaceType.FullName}'. Ensure CoreOne.Generators is referenced.");

        return RegisterRestClient<TInterface>(services, interfaceType, clientType!, effectiveConcreteType, configureClient, serializerKey);
    }

    public static IHttpClientBuilder AddRestClient<TInterface, TClient>(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient>? configureClient = null,
        string? serializerKey = null)
        where TInterface : class
        where TClient : class, TInterface
    {
        var interfaceType = typeof(TInterface);
        var clientType = typeof(TClient);
        var proxyName = $"{clientType.Name}Proxy";
        var proxyType = clientType.Assembly.GetTypes()
            .FirstOrDefault(t => t.IsPublic && t.IsClass && !t.IsAbstract && t.Name == proxyName && t.Namespace == clientType.Namespace && t.IsSubclassOf(clientType));

        return RegisterRestClient<TInterface>(services, interfaceType, clientType, proxyType ?? clientType, configureClient, serializerKey);
    }

    public static IServiceCollection AddRestClientsFromAssembly<T>(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient>? configureClient = null)
    {
        var assembly = typeof(T).Assembly;
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = Array.FindAll(ex.Types, t => t is not null)!;
        }

        var addMethod = typeof(HttpServiceCollectionExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(AddRestClient) &&
                        m.GetGenericArguments().Length == 1 &&
                        m.GetParameters().Length == 3 &&
                        m.GetParameters()[1].ParameterType == typeof(Action<IServiceProvider, HttpClient>));

        foreach (var type in types)
        {
            if (!type.IsPublic || !type.IsClass || type.IsAbstract)
                continue;

            var serviceAttr = type.GetCustomAttribute<ServiceAttribute>(false);
            if (serviceAttr?.ServicingType is not { IsInterface: true } ifaceType)
                continue;

            var takesHttpClient = type.GetConstructors()
                .Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(HttpClient)));
            if (!takesHttpClient)
                continue;

            var genericAdd = addMethod.MakeGenericMethod(ifaceType);
            genericAdd.Invoke(null, [services, configureClient, null]);
        }

        return services;
    }

    private static object CreateClientInstance(
        IServiceProvider services,
        Type concreteType,
        HttpClient httpClient,
        ISerializer? serializer)
    {
        var ctor = concreteType.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"No public constructor found on '{concreteType.FullName}'.");

        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            if (paramType == typeof(HttpClient))
            {
                args[i] = httpClient;
            }
            else if (paramType == typeof(IServiceProvider))
            {
                args[i] = services;
            }
            else if (paramType == typeof(ISerializer))
            {
                args[i] = serializer;
            }
            else
            {
                args[i] = services.Resolve(paramType);
            }
        }

        return ctor.Invoke(args);
    }

    private static string GetDefaultClientName(string interfaceName) =>
        interfaceName.Length > 1 && interfaceName[0] == 'I' && char.IsUpper(interfaceName[1])
            ? $"{interfaceName.Substring(1)}Client"
            : $"{interfaceName}Client";

    private static void RegisterInterceptors(IServiceCollection services, Type interfaceType, Type clientType)
    {
        var interceptAttributes = new List<InterceptAttribute>();
        interceptAttributes.AddRange(interfaceType.GetCustomAttributes<InterceptAttribute>(true));
        interceptAttributes.AddRange(clientType.GetCustomAttributes<InterceptAttribute>(true));

        foreach (var method in interfaceType.GetMethods())
            interceptAttributes.AddRange(method.GetCustomAttributes<InterceptAttribute>(true));

        foreach (var method in clientType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            interceptAttributes.AddRange(method.GetCustomAttributes<InterceptAttribute>(true));

        foreach (var attr in interceptAttributes)
        {
            var descriptor = ServiceDescriptor.Describe(attr.InterceptorType, attr.InterceptorType, attr.Lifetime);
            services.TryAdd(descriptor);
        }
    }

    private static IHttpClientBuilder RegisterRestClient<TInterface>(
        IServiceCollection services,
        Type interfaceType,
        Type clientType,
        Type effectiveConcreteType,
        Action<IServiceProvider, HttpClient>? configureClient,
        string? serializerKey)
        where TInterface : class
    {
        var restAttr = interfaceType.GetCustomAttribute<RestClientAttribute>(true);
        var clientName = !string.IsNullOrWhiteSpace(restAttr?.ClientName)
            ? restAttr!.ClientName!
            : interfaceType.FullName ?? interfaceType.Name;

        var effectiveSerializerKey = serializerKey ?? restAttr?.SerializerKey;
        var serviceAttr = clientType.GetCustomAttribute<ServiceAttribute>(false);
        var lifetime = serviceAttr?.Lifetime ?? ServiceLifetime.Scoped;

        RegisterInterceptors(services, interfaceType, clientType);

        var builder = configureClient is not null
            ? services.AddHttpClient(clientName, configureClient)
            : services.AddHttpClient(clientName);

        Func<IServiceProvider, TInterface> factory = sp => {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(clientName);
            var serializer = !string.IsNullOrWhiteSpace(effectiveSerializerKey)
                ? sp.ResolveSerializer(effectiveSerializerKey)
                : null;
            return (TInterface)CreateClientInstance(sp, effectiveConcreteType, httpClient, serializer);
        };

        var existing = services.FirstOrDefault(d => d.ServiceType == interfaceType);
        if (existing is not null)
            services.Remove(existing);

        services.Add(ServiceDescriptor.Describe(interfaceType, factory, lifetime));
        return builder;
    }

    private static (Type? ClientType, Type? ProxyType) ResolveGeneratedClientTypes(Type interfaceType)
    {
        var restAttr = interfaceType.GetCustomAttribute<RestClientAttribute>(true);
        var expectedClientName = !string.IsNullOrWhiteSpace(restAttr?.ClientName)
            ? restAttr!.ClientName!
            : GetDefaultClientName(interfaceType.Name);

        Type[] types;
        try
        {
            types = interfaceType.Assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = Array.FindAll(ex.Types, t => t is not null)!;
        }

        var clientType = types.FirstOrDefault(t =>
            t.IsPublic && t.IsClass && !t.IsAbstract &&
            t.Name == expectedClientName &&
            t.Namespace == interfaceType.Namespace &&
            interfaceType.IsAssignableFrom(t));

        clientType ??= types.FirstOrDefault(t =>
            t.IsPublic && t.IsClass && !t.IsAbstract &&
            interfaceType.IsAssignableFrom(t) &&
            t.GetCustomAttribute<ServiceAttribute>(false)?.ServicingType == interfaceType);

        if (clientType is null)
            return (null, null);

        var expectedProxyName = $"{clientType.Name}Proxy";
        var proxyType = types.FirstOrDefault(t =>
            t.IsPublic && t.IsClass && !t.IsAbstract &&
            t.Name == expectedProxyName &&
            t.Namespace == clientType.Namespace &&
            t.IsSubclassOf(clientType));

        return (clientType, proxyType);
    }
}
