namespace OneCore.Extensions;

public static class ServiceProviderExtensions
{
    private class CtorInfo(ConstructorInfo? info, Create create)
    {
        public Create Create { get; } = create;
        public ConstructorInfo? Ctor { get; } = info;
        public ParameterInfo[] Parameters { get; } = info?.GetParameters() ?? [];
    }

    private delegate object? Create(object?[] parameters);

    private static readonly Data<Type, CtorInfo> Cache = [];
    private static readonly Lazy<Data<Type, Func<IServiceProvider, Type, object?>>> KnownFactory = new(InitializeKnownFactory);
    private static readonly object Sync = new();

    private static MethodInfo? Method;

    public static TService? GetKnownService<TService>(this IServiceProvider services, [NotNullWhen(true)] bool required = true) where TService : class => (TService?)GetKnownService(services, typeof(TService), required);

    public static object? GetKnownService(this IServiceProvider services, Type type, [NotNullWhen(true)] bool required = true)
    {
        var service = services.GetService(type);
        service ??= CreateInstance(services, type, required);
        return service;
    }

    private static object? CreateInstance(IServiceProvider services, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, [NotNullWhen(true)] bool required)
    {
        if (!Cache.TryGetValue(type, out var info))
        {
            if (type.IsLazyType())
                return Resolve(services, type, required);

            var context = type.GetConstructors()
                .Select(p => new { info = p, parameters = p.GetParameters() })
                .OrderByDescending(p => p.parameters.Length)
                .FirstOrDefault();
            if (context is not null && context.parameters.Length > 0)
            {
                try
                {
                    var targetArgs = context.parameters.Select(p => Resolve(services, p.ParameterType, true)).ToArray();
                    var args = context.parameters.Select(p => p.ParameterType).ToArray();
                    var argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
                    var parammeterExpressions = CreateParameterExpressions(args, argumentsParameter);
                    var ctor = Expression.New(context.info, parammeterExpressions);
                    var lambda = Expression.Lambda<Create>(ctor, argumentsParameter);
                    var func = lambda.Compile();
                    if (func is not null)
                    {
                        info = new CtorInfo(context.info, func);
                        lock (Sync)
                            Cache.Set(type, info);
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
            }
            else
            {
                info = new CtorInfo(null, p => {
                    return type != Types.Object && KnownFactory.Value.TryGetValue(type, out var create) && create is not null
                        ? (create?.Invoke(services, type))
                        : Activator.CreateInstance(type);
                });
                lock (Sync)
                    Cache.Set(type, info);
            }
        }
        if (info is not null)
        {
            var parameters = info.Parameters.Select(p => Resolve(services, p.ParameterType, true)).ToArray();
            var instance = info.Create.Invoke(parameters);
            if (instance is not null)
                return instance;
        }

        return required ? throw new InvalidOperationException($"Unable to create instance of type: {type.FullName}") : null;
    }

    private static Expression[] CreateParameterExpressions(Type[] type, Expression argumentsParameter) => type.Select((parameter, index) => Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(index)), parameter)).ToArray();

    private static Lazy<T?> GetLazyInstance<T>(IServiceProvider service, bool required) where T : class
    {
        var lazy = new Lazy<T?>(() => service.GetKnownService<T>(required));
        return lazy;
    }

    private static Data<Type, Func<IServiceProvider, Type, object?>> InitializeKnownFactory()
    {
        var data = new Data<Type, Func<IServiceProvider, Type, object?>> {
            [typeof(IServiceProvider)] = (sp, type) => sp,
            [typeof(AToken)] = (sp, type) => AToken.Create(),
            [typeof(SToken)] = (sp, type) => SToken.Create(),
            [typeof(object)] = ResolveFromType,
            DefaultKey = Types.Object
        };
        return data;

        static object? ResolveFromType(IServiceProvider services, Type type)
        {
            object? target = null;
            try
            { target = services.GetService(type); }
            catch { }
            return target;
        }
    }

    private static object? Resolve(IServiceProvider services, Type type, bool required)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>))
        {
            Method ??= typeof(ServiceProviderExtensions).GetMethod(nameof(GetLazyInstance), BindingFlags.Static | BindingFlags.NonPublic);
            var mt = type.GetGenericArguments().FirstOrDefault();
            if (mt is not null)
            {
                var method = Method?.MakeGenericMethod(mt);
                return method?.Invoke(null, [services, required]);
            }
        }
        return KnownFactory.Value.Get(type)?.Invoke(services, type);
    }
}