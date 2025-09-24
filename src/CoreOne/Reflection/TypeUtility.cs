namespace CoreOne.Reflection;

public static class TypeUtility
{
    private class Empty
    { }

    private static readonly Data<TypedKey, Delegate> CachedActivators = [];
    private static readonly Type TypeEmpty = typeof(Empty);

    public static BindingFlags Flags { get; set; } = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;

    [return: NotNullIfNotNull(nameof(instance))]
    public static IResult<T> BasicMap<T>(object? instance) => BasicMap(instance, typeof(T)).Select(o => (T?)o);

    [return: NotNullIfNotNull(nameof(instance))]
    public static IResult<object?> BasicMap(object? instance, Type toType)
    {
        try
        {
            object? target = null;
            if (instance is not null)
            {
                var meta = MetaType.GetMetadatas(instance.GetType()).ToDictionary();
                var tometa = MetaType.GetMetadatas(toType).ToDictionary();
                target = Activator.CreateInstance(toType);
                if (target is not null)
                {
                    foreach (var m in meta)
                    {
                        if (tometa.TryGetValue(m.Key, out var tm))
                        {
                            try
                            {
                                tm.SetValue(target, m.Value.GetValue(instance));
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message.Contains("Unable to cast object of type ") && tm.FPType is not null)
                                {
                                    var value = BasicMap(m.Value.GetValue(instance), tm.FPType);
                                    tm.SetValue(target, value);
                                }
                            }
                        }
                    }
                }
            }

            return new Result<object?>(target);
        }
        catch (Exception ex)
        {
            return Result.FromException<object?>(ex);
        }
    }

    public static object GetInternalInstance<T1, T2, T3>(Type type, T1? t1, T2? t2, T3? t3)
    {
        var all = new[] { typeof(T1), typeof(T2), typeof(T3) };
        var args = new object?[] { t1, t2, t3 };
        var key = new TypedKey(type, all, Flags, "ctor");
        var invoke = CachedActivators.GetSet(key, () => CreateDelegate<T1, T2, T3>(type));
        return invoke!.DynamicInvoke(t1, t2, t3)!;
    }

    private static CreateInstance<T1, T2, T3> CreateDelegate<T1, T2, T3>(Type type)
    {
        var all = new[] { typeof(T1), typeof(T2), typeof(T3) };
        var args = all.Where(t => t != TypeEmpty).ToArray();
        var ctor = type.GetConstructor(Flags, null, CallingConventions.HasThis, args, [])!;
        var allexpression = all.Select((t, i) => Expression.Parameter(t, "param" + i)).ToArray();
        var parameters = args.Select((t, i) => allexpression[i]).ToArray();
        var ctorexpression = Expression.New(ctor, parameters);
        var lambda = Expression.Lambda<CreateInstance<T1, T2, T3>>(ctorexpression, allexpression);
        return lambda.Compile();
    }

    public static object GetInstance(this Type type) => GetInternalInstance<Empty, Empty, Empty>(type, null, null, null);

    public static object GetInstance<T1>(this Type type, T1 t1) => GetInternalInstance<T1, Empty, Empty>(type, t1, null, null);

    public static object GetInstance<T1, T2>(this Type type, T1 t1, T2 t2) => GetInternalInstance<T1, T2, Empty>(type, t1, t2, null);

    public static object GetInstance<T1, T2, T3>(this Type type, T1 t1, T2 t2, T3 t3) => GetInternalInstance(type, t1, t2, t3);

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
            name = string.Join(".", names.ToList());
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

#if NET9_0_OR_GREATER

    public static object GetUninitializedInstance(Type type) => System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);

#endif

    public static bool IsSubclassOfRawGeneric(Type generic, Type? toCheck)
    {
        while (toCheck != null && toCheck != Types.Object)
        {
            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
            if (generic == cur)
                return true;

            toCheck = toCheck.BaseType;
        }
        return false;
    }

    public static bool IsSystemType(Type type)
    {
        var ns = type?.Namespace;
        return !string.IsNullOrWhiteSpace(ns) && (ns.StartsWith("System") || ns.StartsWith("Microsoft"));
    }

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