namespace CoreOne.Lookups;

public interface ILookupType<T> where T : ILookupType<T>
{
#if NET9_0_OR_GREATER
    static List<T> Items { get; } = [];
#endif
    string Code { get; }
    string? Description { get; }

#if NET9_0_OR_GREATER
    static abstract T? FindType(object? value);

    static abstract void Initialize();
#endif
}