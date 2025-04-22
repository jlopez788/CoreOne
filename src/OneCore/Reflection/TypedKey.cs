using System.Text;

namespace CoreOne.Reflection;

internal readonly struct TypedKey : IEquatable<TypedKey>
{
    private readonly int Code;
    private readonly string RefId;
    private readonly string Value;
    public Type[] Arguments { get; }
    public BindingFlags Flags { get; }
    public string Name { get; }
    public Type Type { get; }

    public TypedKey(Type type, BindingFlags flags, string? name = null) : this(type, [], flags, name)
    {
    }

    public TypedKey(Type type, Type[] arguments, BindingFlags flags, string? name = null)
    {
        RefId = ID.Create().ToShortId();
        Type = type;
        Name = name ?? string.Empty;
        Arguments = arguments;
        Flags = flags;

        var builder = new StringBuilder()
            .Append(type.FullName)
            .Append($"<{Flags}>");

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

    public override bool Equals(object? obj) => obj is TypedKey key && Equals(key);

    public bool Equals([DisallowNull] TypedKey other) => RefId == other.RefId || Value == other.Value;

    public override int GetHashCode() => Code;

    public override string ToString() => Value;
}