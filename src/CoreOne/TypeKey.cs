using CoreOne.Converters;

namespace CoreOne;

[NJsonConverter(typeof(TypeKeyConverter.NewtonsoftConverter))]
[SJsonConverter(typeof(TypeKeyConverter.SystemJsonConverter))]
public readonly struct TypeKey : IEquatable<TypeKey>
{
    private record Key(string Name, bool Known);
    private static readonly Data<Key, TypeKey> CoreTypes = [];
    private static readonly SafeLock Sync = new();
    private readonly int Code;
    public static TypeKey Empty { get; } = new(Types.Void);
    public string Name { get; }
    public IReadOnlyList<Type> Parameters { get; }
    public Type Type { get; }

    public TypeKey(Type? type, IEnumerable<Type>? arguments = null)
    {
        Type = type ?? Types.Void;
        Name = Type.Name;
        Parameters = (arguments ?? []).ToList();
        Code = Parameters.Count == 0 && Type == Types.Void ? 0 :
            Type != Types.Void && Parameters.Count == 0 ?
            Type.GetHashCode() : (Type, Parameters).GetHashCode();
        RegisterType(this, true);
    }

    public static TypeKey FindType(string name)
    {
        var key = new Key(name, true);
        if (CoreTypes.TryGetValue(key, out var type))
            return type;
        key = new Key(name, false);
        return CoreTypes.TryGetValue(key, out type) ? type : Empty;
    }

    public static implicit operator Type(TypeKey type) => type.Type;

    public static implicit operator TypeKey(Type type) => new(type);

    public static bool operator !=(TypeKey left, TypeKey right) => !(left == right);

    public static bool operator ==(TypeKey left, TypeKey right) => left.Equals(right);

    public static void Register<T>() => RegisterType(new TypeKey(typeof(T)), true);

    public static void Register(Type type) => RegisterType(new TypeKey(type), true);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj switch {
        TypeKey key => Equals(key),
        string name => Name.Matches(name),
        _ => false
    };

    public bool Equals(TypeKey other) => Type == other.Type || (Name.Matches(other.Name) && (Type == other.Type || Type == Types.Void) && Parameters.Count == other.Parameters.Count && Parameters.SequenceEqual(other.Parameters));

    public override int GetHashCode() => Code;

    public override string ToString() => Name;

    private static void RegisterType(TypeKey type, bool known)
    {
        var key = new Key(type.Name, known);
        if (!CoreTypes.ContainsKey(key))
        {
            lock (Sync)
            {
                if (!CoreTypes.ContainsKey(key))
                    CoreTypes.Add(key, type);
            }
        }
    }
}