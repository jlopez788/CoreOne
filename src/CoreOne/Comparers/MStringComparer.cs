namespace CoreOne.Comparers;

/// <summary>
/// String comparer that equates null string and empty string
/// </summary>
public sealed class MStringComparer : IEqualityComparer<string?>, IComparer<string?>, IEqualityComparer
{
    public static readonly MStringComparer Ordinal = new(false);
    public static readonly MStringComparer OrdinalIgnoreCase = new(true);
    private readonly bool IgnoreCase;

    private MStringComparer(bool ignoreCase) => IgnoreCase = ignoreCase;

    public int Compare(string? x, string? y)
    {
        x ??= string.Empty;
        y ??= string.Empty;
        return string.Compare(x, y, IgnoreCase);
    }

    public bool Equals(string? x, string? y) => Compare(x, y) == 0;

    public new bool Equals(object? x, object? y) => Compare(x?.ToString(), y?.ToString()) == 0;

    public int GetHashCode(string? value)
    {
        var comparer = IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        return comparer.GetHashCode(value ?? string.Empty);
    }
    
    public int GetHashCode(object obj) => GetHashCode(obj.ToString());
}