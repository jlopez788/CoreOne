namespace OneCore.Lookups;

public interface ILookupType<T> where T : ILookupType<T>
{
    static abstract void Initialize();

    static abstract T? FindType(object? value);

    static List<T> Items { get; } = [];
    string Code { get; }
    string? Description { get; }
}