namespace CoreOne.ODataBuilders;

public readonly struct FilterTypeResult(bool parsed, bool isEmpty, object? value, string? filterValue)
{
    public static readonly FilterTypeResult Empty = new(false, true, null, null);
    public bool IsEmpty { get; } = isEmpty;
    public bool Parsed { get; } = parsed;
    public object? Value { get; } = value;
    public string? GivenValue { get; } = filterValue ?? value?.ToString();
}