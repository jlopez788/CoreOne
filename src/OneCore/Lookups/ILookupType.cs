namespace CoreOne.Lookups;

public interface ILookupType<T> where T : ILookupType<T>
{
    static List<T> Items { get; } = [];
    string Code { get; }
    string? Description { get; }

    static abstract T? FindType(object? value);

    static abstract void Initialize();
}