using System.Diagnostics;

namespace CoreOne.Models;

[DebuggerDisplay("{DebugLabel}")]
public sealed class NamedKey : IEquatable<NamedKey>
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)] private readonly Data<string, object?> Entries;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)] private int Hashcode;

    private string DebugLabel {
        get {
            if (Entries.Count == 0)
                return "--Empty--";
            var first = Entries.First();
            var more = Entries.Count > 1 ? $"....({Entries.Count}+)" : string.Empty;
            return $"{first.Key}: {first.Value}{more}";
        }
    }

    public object? this[string key] {
        get => Entries.Get(key);
        set => Set(key, value);
    }

    public NamedKey() => Entries = new Data<string, object?>(MStringComparer.OrdinalIgnoreCase);

    public static bool operator !=(NamedKey? left, NamedKey? right) => !(left == right);

    public static bool operator ==(NamedKey? left, NamedKey? right) => left?.Equals(right) == true;

    public void Add(string key, object? value) => Set(key, value);

    public bool ContainsKey(string? key) => key is not null && Entries.ContainsKey(key);

    public override bool Equals(object? obj) => obj is NamedKey key && Equals(key);

    public bool Equals(NamedKey? other)
    {
        if (other is not null && other.Entries.Count == Entries.Count)
        {
            if (Entries.Count == 0)
                return true; // Nothing to compare; both empty

            var checkedKey = new HashSet<string>(MStringComparer.OrdinalIgnoreCase);
            foreach (var key in Entries.Keys)
            {
                if (other.Entries.TryGetValue(key, out var value) && Compare(Entries[key], value))
                    checkedKey.Add(key);
                else
                    return false;
            }

            var otherKeys = other.Entries.Keys.Where(p => !checkedKey.Contains(p));
            foreach (var key in otherKeys)
            {
                if (!Entries.TryGetValue(key, out var value) || !Compare(value, other.Entries[key]))
                    return false;
            }
            return true;
        }

        return false;

        static bool Compare(object? x, object? y) => x?.Equals(y) == true || ReferenceEqualityComparer.Default.Equals(x, y);
    }

    public IReadOnlyDictionary<string, object?> GetEntries() => new Data<string, object?>(Entries);

    public override int GetHashCode() => Hashcode;

    public void Set(string key, object? value)
    {
        Entries.Set(key, value);
        Hashcode = Entries.Count == 0 ? 0 : Crc32.Compute(Utility.Serialize(this, false));
    }
}