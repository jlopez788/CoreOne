using CoreOne.Threading.Tasks;

namespace CoreOne.Reflection;

public readonly struct Metadata : IEquatable<Metadata>
{
    public static readonly Metadata Empty = new(null, null, null, null);
    private readonly MemberInfo? Member;
    private readonly string RefId;
    public bool CanRead { get; }
    public bool CanWrite { get; }
    public Type FPType { get; }
    public InvokeCallback Getter { get; }
    public string Name { get; }
    public Set? Setter { get; }

    public Metadata(MemberInfo? member, Type? fpType, Get? getter, Set? setter)
    {
        RefId = (member is null ? Guid.Empty : ID.Create()).ToShortId();
        Name = member?.Name ?? string.Empty;
        FPType = fpType ?? Types.Void;
        Member = member;
        Getter = getter is null ? InvokeCallback.Empty : new InvokeCallback(getter);
        Setter = setter;
        CanRead = true;
        CanWrite = true;
        if (member is PropertyInfo p)
        {
            CanRead = p.CanRead;
            CanWrite = p.CanWrite;
        }
    }

    public static bool operator !=(Metadata x, Metadata y) => !x.Equals(y);

    public static bool operator ==(Metadata x, Metadata y) => x.Equals(y);

    public FieldInfo? AsFieldInfo() => Member as FieldInfo;

    public PropertyInfo? AsPropertyInfo() => Member as PropertyInfo;

    public override bool Equals(object? obj) => obj is Metadata meta && Equals(meta);

    public bool Equals([AllowNull] Metadata other) => RefId == other.RefId || (Member is null && other.Member is null) || (Member is not null && Member.Equals(other.Member));

    public T? GetCustomAttribute<T>() where T : Attribute => Member?.GetCustomAttribute<T>();

    public T? GetCustomAttribute<T>(bool inherits) where T : Attribute => Member?.GetCustomAttribute<T>(inherits);

    public IEnumerable<T> GetCustomAttributes<T>() where T : Attribute => Member?.GetCustomAttributes<T>() ?? [];

    public IEnumerable<T> GetCustomAttributes<T>(bool inherits) where T : Attribute => Member?.GetCustomAttributes<T>(inherits) ?? [];

    public override int GetHashCode() => Member?.GetHashCode() ?? 0;

    public object? GetValue(object? instance) => CanRead && instance is not null ? Getter.Invoke(instance, null) : null;

    public bool SetValue(object? instance, object? value)
    {
        var set = false;
        try
        {
            if (CanWrite && Setter is not null && instance is not null)
            {
                Setter.Invoke(instance, value);
                set = true;
            }
        }
        catch { }
        return set;
    }

    public override string ToString() => Name;

    public bool TryGetAttribute<T>([NotNullWhen(true)] out T? attribute) where T : Attribute
    {
        attribute = Member?.GetCustomAttribute<T>();
        return attribute is not null;
    }
}