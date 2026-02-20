using System.Diagnostics;
using System.Text;

namespace CoreOne.Reflection;

[DebuggerDisplay("{Value}")]
public readonly struct TypeKey : IEquatable<TypeKey>
{
    public static readonly TypeKey Empty = new();
    private readonly int Code;
    private readonly string RefId;
    private readonly string Value;
    public Type[] Arguments { get; }
    public string Name { get; }
    public Type Type { get; }

    public TypeKey(Type type, string? name = null) : this(type, [], name)
    {
    }

    public TypeKey(Type type, Type[]? arguments, string? name = null)
    {
        RefId = ID.Create().ToShortId();
        Type = type;
        Name = name ?? type.Name;
        Arguments = arguments ?? [];

        var builder = new StringBuilder()
            .Append(type.FullName);

        if (!string.IsNullOrEmpty(name))
        {
            builder.Append("::")
                .Append(name);
        }
        if (Arguments.Length > 0)
        {
            var args = string.Join(", ", Arguments.Select(p => p.FullName));
            builder.AppendFormat("({0})", args);
        }

        Value = builder.ToString();
        Code = StringComparer.Ordinal.GetHashCode(Value);
    }

    public TypeKey()
    {
        Code = 0;
        RefId = string.Empty;
        Value = string.Empty;
        Arguments = [];
        Name = string.Empty;
        Type = Types.Void;
    }

    public static bool operator !=(TypeKey left, TypeKey right) => !(left == right);

    public static bool operator ==(TypeKey left, TypeKey right) => left.Equals(right);

    public override bool Equals(object? obj) => obj is TypeKey key && Equals(key);

    public bool Equals(TypeKey other) => RefId == other.RefId || Value == other.Value;

    public override int GetHashCode() => Code;

    public override string ToString() => Value;
}