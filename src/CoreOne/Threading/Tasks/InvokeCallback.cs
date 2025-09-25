using System.Diagnostics;

namespace CoreOne.Threading.Tasks;

[DebuggerDisplay("{Name}")]
public class InvokeCallback(MulticastDelegate? method)
{
    public static readonly InvokeCallback Empty = new(Guid.Empty, null, "Empty");
    public static readonly InvokeCallback Identity = new(new Guid("11111111-1111-1111-1111-111111111111"), new InvokeReturn((target, args) => target), "Identity");
    private readonly Guid Id = method is null ? Guid.Empty : (Guid)ID.CreateV7();
    private readonly MulticastDelegate? Method = method;
    private readonly string Name = method is null ? "Empty" : method.GetType().Name;
    public bool IsEmpty { get; } = method is null;

    private InvokeCallback(Guid id, MulticastDelegate? invoke, string name) : this(invoke)
    {
        Id = id;
        Name = name;
    }

    public static implicit operator InvokeCallback(MulticastDelegate method) => new(method);

    public static bool operator !=(InvokeCallback left, InvokeCallback right) => !(left == right);

    public static bool operator ==(InvokeCallback left, InvokeCallback right) => (left is null && right is null) || left?.Equals(right) == true;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is InvokeCallback invoke && Equals(invoke);

    public bool Equals(InvokeCallback? invoke) => Id == invoke?.Id || IsEmpty == invoke?.IsEmpty;

    public override int GetHashCode() => Id.GetHashCode();

    public object? Invoke(object?[] arguments) => InvokeInternal(null, arguments);

    public object? Invoke(object? instance, object?[]? arguments = null) => InvokeInternal(instance, arguments);

    public SafeTask InvokeAsync(object?[]? arguments = null) => InvokeAsync(null, arguments);

    public SafeTask InvokeAsync(object? instance, object?[]? arguments = null)
    {
        var result = InvokeInternal(instance, arguments);
        return new SafeTask(result);
    }

    public override string ToString() => Name;

    private object? InvokeInternal(object? instance, object?[]? arguments)
    {
        return Method switch {
            InvokeReturn invoke => instance is not null ? invoke.Invoke(instance, arguments ?? []) : null,
            InvokeStatic invoke => arguments is not null ? invoke.Invoke(arguments) : null,
            InvokeStaticVoid invoke => invokeStaticVoid(invoke),
            InvokeVoid invoke => invokeVoid(invoke),
            Get invoke => instance != null ? invoke.Invoke(instance) : null,
            _ => invokeDefaultDelegate(mergeArguments())
        };
        object? invokeStaticVoid(InvokeStaticVoid invoke)
        {
            if (arguments is not null)
                invoke.Invoke(arguments);
            return null;
        }
        object? invokeVoid(InvokeVoid invoke)
        {
            if (instance is not null)
                invoke.Invoke(instance, arguments);
            return null;
        }
        object? invokeDefaultDelegate(object?[]? arguments)
        {
            var parameters = Method?.Method?.GetParameters();
            var len = parameters?.Length ?? 0;
            var array = new object?[len];

            if (arguments is not null && len <= arguments.Length)
            {
#if NET9_0_OR_GREATER
                var nullable = new NullabilityInfoContext();
#endif
                Array.Copy(arguments, array, len);
                for (int i = 0; i < len; i++)
                {
                    var expectedType = parameters![i].ParameterType;
                    var actualValue = array[i];
#if NET9_0_OR_GREATER
                    var info = nullable.Create(parameters[i]);
                    if (info.ReadState == NullabilityState.NotNull && actualValue == null)
                        return null;
#endif
                    if (actualValue == null || expectedType.IsInstanceOfType(actualValue))
                        continue;

                    var converted = tryApplyImplicitConversion(actualValue, expectedType);
                    if (converted != null)
                        array[i] = converted;
                    else
                    {
                        array[i] = null;
#if NET9_0_OR_GREATER
                        if (info.ReadState == NullabilityState.NotNull)
                            return null;
#endif
                    }
                }
            }

            return Method?.DynamicInvoke(array);
        }
        object?[] mergeArguments()
        {
            var args = new List<object?>();
            if (instance is not null)
                args.Add(instance);
            if (arguments is not null)
                args.AddRange(arguments);
            return [.. args];
        }
        object? tryApplyImplicitConversion(object input, Type targetType)
        {
            var inputType = input.GetType();
            var ntype = Nullable.GetUnderlyingType(targetType);
            // Implicit operator on input type
            var method = inputType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "op_Implicit" &&
                   (m.ReturnType == targetType || ntype != null && m.ReturnType == ntype) &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType.IsAssignableFrom(inputType));

            if (method != null)
                return method.Invoke(null, [input]);

            // Implicit operator on target type
            method = targetType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "op_Implicit" &&
                    (m.ReturnType == targetType || ntype != null && m.ReturnType == ntype) &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType.IsAssignableFrom(inputType));

            return method?.Invoke(null, [input]);
        }
    }
}