using System.Collections.Concurrent;

namespace CoreOne.Services;

public class TargetCreator(IServiceProvider? oservices)
{
    private class CtorInfo(ConstructorInfo? info, Create create)
    {
        public Create Create { get; } = create;
        public ConstructorInfo? Ctor { get; } = info;
        public ParameterInfo[] Parameters { get; } = info?.GetParameters() ?? [];
    }

    private delegate object? Create(object?[] parameters);

    public static readonly TargetCreator Default = new(null);
    private static readonly Lazy<Data<Type, Func<IServiceProvider?, Type, object?>>> KnownFactory = new(InitializeKnownFactory);
    private static MethodInfo? Method;
    private readonly ConcurrentDictionary<TypeKey, CtorInfo> Cache = new();
    protected IServiceProvider? ServiceProvider { get; } = oservices;

    public IResult<object?> CreateInstance(Type type, object[] parameters)
    {
        var key = new TypeKey(type, parameters.SelectArray(p => p.GetType()));
        var info = Cache.TryGetValue(key, () => CreateConstructorInfo(key, false));
        if (info is not null)
        {
            var target = info.Create.Invoke(parameters);
            return new Result<object?>(target, true);
        }
        return new Result<object?>(ResultType.Fail, "Invalid constructor");
    }

    public object CreateInstance(Type type)
    {
        var args = type.GetConstructors()
               .Select(p => new { ctor = p, parameters = p.GetParameters().SelectArray(a => a.ParameterType) })
               .OrderByDescending(p => p.parameters.Length)
               .FirstOrDefault();
        var key = new TypeKey(type, args?.parameters);
        var info = Cache.TryGetValue(key, () => CreateConstructorInfo(key, true));
        if (info is not null)
        {
            var parameters = info.Parameters.SelectArray(p => Resolve(p.ParameterType, false));
            var target = info.Create.Invoke(parameters);
            if (target is not null)
                return target;
        }

        throw new InvalidOperationException();
    }

    private static Expression[] CreateParameterExpressions(Type[] type, Expression argumentsParameter) => [.. type.Select((parameter, index) => Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(index)), parameter))];

    private static Data<Type, Func<IServiceProvider?, Type, object?>> InitializeKnownFactory()
    {
        var data = new Data<Type, Func<IServiceProvider?, Type, object?>> {
            [typeof(IServiceProvider)] = (sp, type) => sp,
            [typeof(AToken)] = (sp, type) => AToken.Create(),
            [typeof(SToken)] = (sp, type) => SToken.Create(),
            [typeof(object)] = ResolveFromType,
            DefaultKey = Types.Object
        };
        return data;

        static object? ResolveFromType(IServiceProvider? services, Type type)
        {
            object? target = null;
            try
            { target = services?.GetService(type); }
            catch { }
            return target;
        }
    }

    private CtorInfo CreateConstructorInfo(TypeKey key, bool useSP)
    {
        var context = key.Type.GetConstructors()
             .Select(p => new { info = p, parameters = p.GetParameters() })
             .OrderByDescending(p => p.parameters.Length)
             .FirstOrDefault(p => (useSP && ServiceProvider is not null) || p.parameters.Select(n => n.ParameterType).SequenceEqual(key.Parameters));
        if (context is not null)
        {
            var argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
            var parammeterExpressions = CreateParameterExpressions([.. key.Parameters], argumentsParameter);
            var ctor = Expression.New(context.info, parammeterExpressions);
            var lambda = Expression.Lambda<Create>(ctor, argumentsParameter);
            var func = lambda.Compile();
            if (func is not null)
                return new CtorInfo(context.info, func);
        }

        return new CtorInfo(null, p => Activator.CreateInstance(key.Type));
    }

    private Lazy<T?> GetLazyInstance<T>(bool required) where T : class => new(() => (T?)Resolve(typeof(T), required));

    private object? Resolve(Type type, bool required)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>))
        {
            Method ??= typeof(ServiceProviderExtensions).GetMethod(nameof(GetLazyInstance), BindingFlags.Instance | BindingFlags.NonPublic);
            var mt = type.GetGenericArguments().FirstOrDefault();
            if (mt is not null)
            {
                var method = Method?.MakeGenericMethod(mt);
                return method?.Invoke(this, [required]);
            }
        }
        return KnownFactory.Value.Get(type)?.Invoke(ServiceProvider, type);
    }
}