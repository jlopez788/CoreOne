namespace CoreOne.Lookups;

public interface ILookupType
{
    string Code { get; }
    string? Description { get; }
}

public interface ILookupType<T> : ILookupType where T : ILookupType<T>
{
#if NET9_0_OR_GREATER
    static List<T> Items { get; } = [];

    static abstract T? FindType(object? value);

    static abstract void Initialize();

#endif
}