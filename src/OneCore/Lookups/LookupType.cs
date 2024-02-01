using System.Reflection;

namespace OneCore.Lookups;

public class LookupType<T> : ILookupType<T> where T : LookupType<T>
{
    public static List<T> Items { get; private set; } = [];
    public string Code { get; set; }
    public string? Description { get; set; }

    static LookupType() => Initialize();

    public LookupType() => Code = string.Empty;

    protected LookupType(string code, string? description = null)
    {
        Code = code;
        Description = description ?? code;
    }

    public static T? FindType(object? value)
    {
        if (Items.Count <= 1)
            Initialize();

        return Items.FirstOrDefault(t => t.Equals(value));
    }

    public static void Initialize() => Initialize(typeof(T));

    public override bool Equals(object? obj)
    {
        return obj switch {
            string str => string.Compare(Code, str, true) == 0,
            LookupType<T> lookup => string.Compare(Code, lookup.Code, true) == 0,
            _ => false,
        };
    }

    public override int GetHashCode() => Code?.GetHashCode() ?? 0;

    protected static void Initialize(Type type)
    {
        var properties = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        if (properties != null)
        {
            Items = new List<T>(properties.Select(p => p.GetValue(null) as T).ExcludeNulls());
        }
    }
}