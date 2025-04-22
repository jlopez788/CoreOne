using System.Collections.Concurrent;

namespace CoreOne.Reflection;

public static class MetaType
{
    private readonly struct Key : IEquatable<Key>
    {
        private readonly string Value;
        public IReadOnlyList<Type> Arguments { get; }
        public string Name { get; }
        public Type Type { get; }

        public Key(Type type, string name)
        {
            Type = type;
            Name = name;
            Arguments = [];
            Value = $"{type?.FullName}::{name}";
        }

        public Key(Type type, Type[]? arguments, string name)
        {
            Type = type;
            Name = name;
            Arguments = arguments?.ToList() ?? [];
            var args = string.Join(", ", Arguments.Select(p => p.FullName));
            Value = $"{type?.FullName}::{name}({args})";
        }

        public override bool Equals(object? obj) => obj is Key key && Equals(key);

        public bool Equals([AllowNull] Key other)
        {
            var equals = Type == other.Type && Name.Matches(other.Name);
            if (equals && Arguments.Count > 0)
            {
                equals = false;
                if (Arguments.Count == other.Arguments.Count)
                    equals = Arguments.SequenceEqual(other.Arguments);
            }
            return equals;
        }

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public override string ToString() => Value;
    }

    [Flags]
    private enum ActionType
    { Read = 0x01, Write = 0x10 }

    public const BindingFlags FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase;
    public const BindingFlags INSTANCE_FLAGS = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
    private static readonly ConcurrentDictionary<Key, Metadata> Cache = new();
    private static readonly ConcurrentDictionary<Key, List<Metadata>> ClassCache = new();
    private static readonly ConcurrentDictionary<Key, InvokeCallback> Method = new();

    public static Metadata CreateFromMemberInfo(Type? type, MemberInfo? member)
    {
        if (type is null || member is null)
            return Metadata.Empty;

        var key = new Key(type, member.Name);
        return Create(key, member);
    }

    public static InvokeCallback GetHandler(Type type, string? propertyName, BindingFlags flags = FLAGS)
    {
        var meta = GetMetadata(type, propertyName, flags);
        return meta.Getter;
    }

    public static InvokeCallback GetInvokeMethod(MethodInfo? method)
    {
        if (method is not null)
        {
            if (method.IsStatic)
            {
                var argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
                var call = Expression.Call(method, CreateParameterExpressions(method, argumentsParameter));
                if (method.ReturnType != Types.Void)
                {
                    var lambda = Expression.Lambda<InvokeStatic>(Expression.Convert(call, typeof(object)), argumentsParameter);
                    return new InvokeCallback(lambda.Compile());
                }
                else
                {
                    var lambda = Expression.Lambda<InvokeStaticVoid>(call, argumentsParameter);
                    return new InvokeCallback(lambda.Compile());
                }
            }
            else
            {
                var instanceParameter = Expression.Parameter(typeof(object), "target");
                var argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
                var convert = Expression.Convert(instanceParameter, method.DeclaringType!);
                var call = Expression.Call(convert, method, CreateParameterExpressions(method, argumentsParameter));
                if (method.ReturnType != Types.Void)
                {
                    var lambda = Expression.Lambda<InvokeReturn>(Expression.Convert(call, typeof(object)), instanceParameter, argumentsParameter);
                    return new InvokeCallback(lambda.Compile());
                }
                else
                {
                    var lambda = Expression.Lambda<InvokeVoid>(call, instanceParameter, argumentsParameter);
                    return new InvokeCallback(lambda.Compile());
                }
            }
        }
        return InvokeCallback.Empty;
    }

    public static InvokeCallback GetInvokeMethod(Type? type, string name, params Type[] args)
    {
        if (type is null)
            return InvokeCallback.Empty;

        var key = new Key(type, args, name);
        if (Method.TryGetValue(key, out var invoke))
            return invoke;

        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        var method = type.GetMethod(name, flags, Type.DefaultBinder, args, null);
        invoke = GetInvokeMethod(method);
        Method.TryAdd(key, invoke);

        return invoke;
    }

    public static InvokeCallback GetInvokestaticMethod(Type type, string name, params Type[] args)
    {
        var key = new Key(type, args, name);
        if (Method.TryGetValue(key, out var invoke))
            return invoke;

        var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        var method = type.GetMethod(name, flags, Type.DefaultBinder, args, null);
        if (method != null)
        {
            invoke = GetInvokeMethod(method);
            Method.TryAdd(key, invoke);
            return invoke;
        }
        return InvokeCallback.Empty;
    }

    public static Metadata GetMetadata(Type? type, string? propertyName, BindingFlags flags = FLAGS)
    {
        if (type is null || string.IsNullOrEmpty(propertyName))
            return Metadata.Empty;

        var meta = Metadata.Empty;
        var key = new Key(type, "");
        if (ClassCache.TryGetValue(key, out var props) && props?.Count > 0)
        { // See if we have ran it from the class previously
            meta = props.FirstOrDefault(p => p.Name.Matches(propertyName));
            return meta;
        }

        key = new(type, propertyName ?? "");
        if (Cache.TryGetValue(key, out meta))
            return meta;

        meta = Create(key, flags);
        if (!Metadata.Empty.Equals(meta))
            Cache.TryAdd(key, meta);
        return meta;
    }

    public static IReadOnlyList<Metadata> GetMetadatas(Type type, BindingFlags flags = INSTANCE_FLAGS)
    {
        var entries = new List<Metadata>(15);
        if (type is not null)
        {
            var key = new Key(type, "");
            if (ClassCache.TryGetValue(key, out entries))
                return entries;

            var properties = type.GetProperties(flags);
            entries ??= new List<Metadata>(15);
            entries.AddRange(properties.Select(p => new Key(type, p.Name))
                   .Select(p => Create(p, flags)));

            var fields = type.GetFields(flags);
            entries.AddRange(fields.Select(p => new Key(type, p.Name))
                .Select(p => Create(p, flags)));

            entries.RemoveAll(p => Metadata.Empty.Equals(p));
            if (entries.Count > 0)
                ClassCache.TryAdd(key, entries);
        }
        return entries;
    }

    public static string GetName<TProperty>(this Expression<Func<TProperty>>? expression)
    {
        if (expression is null)
            return string.Empty;

        string name = string.Empty;
        if (TryFindMemberExpression(expression.Body, out MemberExpression? member))
        {
            var names = new Stack<string>();
            do
            { names.Push(member.Member.Name); }
            while (TryFindMemberExpression(member.Expression, out member));
            name = string.Join(".", names.ToArray());
        }
        return name;
    }

    public static string? GetName<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyLambda)
    {
        Type type = typeof(TSource);

        if (propertyLambda.Body is not MemberExpression member)
            throw new ArgumentException(string.Format(
                "Expression '{0}' refers to a method, not a property.",
                propertyLambda.ToString()));

        PropertyInfo? propInfo = member?.Member as PropertyInfo;
        return propInfo == null
            ? throw new ArgumentException(string.Format("Expression '{0}' refers to a field, not a property.", propertyLambda.ToString()))
            : type != propInfo.ReflectedType && propInfo.ReflectedType is not null && !type.IsSubclassOf(propInfo.ReflectedType)
            ? throw new ArgumentException(string.Format("Expression '{0}' refers to a property that is not from type {1}.", propertyLambda.ToString(), type))
            : (propInfo?.Name);
    }

    public static PropertyInfo? GetPropertyInfo<TProperty>(Expression<Func<TProperty>> expression)
    {
        var member = expression.Body as MemberExpression;
        return member?.Member as PropertyInfo;
    }

    [return: NotNullIfNotNull(nameof(type))]
    public static Type? GetUnderlyingType(Type? type)
    {
        if (type == null)
            return null;

        var generic = type.IsGenericType ? type.GetGenericArguments() : null;
        return generic?.Length > 0 ? GetUnderlyingType(generic[0]) : (type is not null ? Nullable.GetUnderlyingType(type) ?? type : type);
    }

    public static Set? SetHandler(Type type, string propertyName, BindingFlags flags = FLAGS)
    {
        var meta = GetMetadata(type, propertyName, flags);
        return meta.Setter;
    }

    private static Metadata Create(Key key, BindingFlags flags)
    {
        var property = key.Type?.GetProperty(key.Name, flags);
        if (property is not null)
            return Create(key, property);

        var field = key.Type?.GetField(key.Name, flags);
        return field is not null ? Create(key, field) : Metadata.Empty;
    }

    private static Metadata Create(Key key, MemberInfo member)
    {
        ActionType actions;
        var tobject = typeof(object);
        var oinstance = Expression.Parameter(tobject, "instance");
        if (member is PropertyInfo property)
        {
            var objtotyp = Expression.Convert(oinstance, key.Type!);
            var mexp = Expression.Property(objtotyp, property);
            actions = getAction(property.CanRead, property.CanWrite);
            return create(property, mexp, property.PropertyType, actions);
        }
        else if (member is FieldInfo field)
        {
            var objtotyp = Expression.Convert(oinstance, key.Type!);
            var mexp = Expression.Field(objtotyp, field);
            actions = getAction(true, true);
            return create(field, mexp, field.FieldType, actions);
        }

        return Metadata.Empty;

        ActionType getAction(bool canRead, bool canWrite)
        {
            ActionType action = 0;
            if (canRead)
                action |= ActionType.Read;
            if (canWrite)
                action |= ActionType.Write;
            return action;
        }
        Metadata create(MemberInfo member, MemberExpression mexp, Type type, ActionType action)
        {
            var typtoobj = Expression.Convert(mexp, tobject);
            var value = Expression.Parameter(tobject, "value");
            var valtotyp = Expression.Convert(value, type);
            Set? set = null;
            Get? get = null;
            if ((action & ActionType.Read) == ActionType.Read)
            {
                var lambda = Expression.Lambda<Get>(typtoobj, oinstance);
                get = lambda.Compile();
            }
            if ((action & ActionType.Write) == ActionType.Write)
            {
                try
                {
                    var lambdaSet = Expression.Lambda<Set>(Expression.Assign(mexp, valtotyp), oinstance, value);
                    set = lambdaSet.Compile();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("err " + ex.Message);
                }
            }

            return new Metadata(member, type, get, set);
        }
    }

    private static Expression[] CreateParameterExpressions(MethodInfo method, Expression argumentsParameter) => [.. method.GetParameters().Select((parameter, index) => Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(index)), parameter.ParameterType))];

    private static bool IsConversion(Expression? exp) => exp is not null && (exp.NodeType == ExpressionType.Convert || exp.NodeType == ExpressionType.ConvertChecked);

    private static bool TryFindMemberExpression(Expression? exp, [NotNullWhen(true)] out MemberExpression? memberExp)
    {
        memberExp = exp as MemberExpression;
        if (memberExp is not null)
        {
            return true;
        }

        // if the compiler created an automatic conversion,
        // it'll look something like...
        // obj => Convert(obj.Property) [e.g., int -> object]
        // OR:
        // obj => ConvertChecked(obj.Property) [e.g., int -> long]
        // ...which are the cases checked in IsConversion
        if (IsConversion(exp) && exp is UnaryExpression expression)
        {
            memberExp = expression.Operand as MemberExpression;
            if (memberExp is not null)
            {
                return true;
            }
        }
        return false;
    }
}