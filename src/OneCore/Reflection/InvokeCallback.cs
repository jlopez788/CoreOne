using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CoreOne.Reflection;

[DebuggerDisplay("{Name}")]
public readonly struct InvokeCallback
{
    public class SafeTask
    {
        private readonly TaskCompletionSource<object?> Task;

        public SafeTask(object? result)
        {
            Task = new();
            if (result is Task task)
            {
                task.ContinueWith(p => {
                    try
                    {
                        var meta = MetaType.GetMetadata(p.GetType(), nameof(Result));
                        if (meta != Metadata.Empty)
                            Task.SetResult(meta.GetValue(p));
                        else
                            Task.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        Task.SetException(ex);
                    }
                });
            }
            else
                Task.SetResult(result);
        }

        public TaskAwaiter<object?> GetAwaiter() => Task.Task.GetAwaiter();
    }

    public static readonly InvokeCallback Empty = new(Guid.Empty, null, "Empty");
    public static readonly InvokeCallback Identity = new(new Guid("11111111-1111-1111-1111-111111111111"), new InvokeReturn((target, args) => target), "Identity");
    private readonly Guid Id;
    private readonly MulticastDelegate? Method;
    private readonly string Name;
    public bool IsEmpty { get; }

    public InvokeCallback(MulticastDelegate? invoke)
    {
        Method = invoke;
        IsEmpty = invoke is null;
        Name = invoke is null ? "Empty" : invoke.GetType().Name;
        Id = IsEmpty ? Guid.Empty : Guid.NewGuid();
    }

    private InvokeCallback(Guid id, MulticastDelegate? invoke, string name)
    {
        Id = id;
        Name = name;
        Method = invoke;
        IsEmpty = invoke is null;
    }

    public static implicit operator InvokeCallback(MulticastDelegate method) => new(method);

    public static implicit operator MulticastDelegate?(InvokeCallback invoke) => invoke.Method;

    public static bool operator !=(InvokeCallback left, InvokeCallback right) => !(left == right);

    public static bool operator ==(InvokeCallback left, InvokeCallback right) => left.Equals(right);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is InvokeCallback invoke && Equals(invoke);

    public bool Equals(InvokeCallback invoke) => Id == invoke.Id || IsEmpty == invoke.IsEmpty;

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
            _ => null
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
    }
}