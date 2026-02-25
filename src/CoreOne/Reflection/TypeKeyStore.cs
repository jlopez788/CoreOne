using System.Collections.Concurrent;

namespace CoreOne.Reflection;

public static class TypeKeyStore
{
    private static readonly ConcurrentDictionary<string, TypeKey> Data;

    static TypeKeyStore()
    {
        var nullable = typeof(Nullable<>);
        Data = new ConcurrentDictionary<string, TypeKey>();
        Types.LookupTryParse.Value.Each(kp => {
            var key = kp.Key.Name;
            Data.TryAdd(key, new TypeKey(kp.Key));

            key = $"{key}?";
            Data.TryAdd(key, new TypeKey(nullable.MakeGenericType(kp.Key), key));
        });
    }

    public static TypeKey FindType(string? name) => name.IsNotNullOrEmpty() && Data.TryGetValue(name, out var key) ? key : TypeKey.Empty;

    public static IEnumerable<TypeKey> GetKnownTypes() => Data.Values;

    public static TypeKey Register<T>(string? name = null) => Register(typeof(T), name);

    public static TypeKey Register(Type? type, string? name = null)
    {
        if (type is null)
            return TypeKey.Empty;

        var key = new TypeKey(type, name);
        Data.TryAdd(key.Name, key);
        return key;
    }
}