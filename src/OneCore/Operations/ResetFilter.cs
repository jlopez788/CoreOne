namespace OneCore.Operations;

public class ResetFilter(string? field) : IOperation
{
    public string Field { get; } = field ?? string.Empty;

    public override bool Equals(object? obj) => obj is ResetFilter filter && Field.Matches(filter.Field);

    public override int GetHashCode() => Field.GetHashCode();

    public override string ToString() => $"Reset Filter: {Field}";
}
 