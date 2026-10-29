namespace CoreOne.Operations;

public class ResetFilter(string? field) : IOperation
{
    public string Field { get; } = field ?? string.Empty;

    public bool Apply(List<IOperation> operations)
    {
        operations.RemoveAll(p => p is FilterBy fb && fb.Field.Matches(Field));
        return true;
    }

    public override bool Equals(object? obj) => obj is ResetFilter filter && Field.Matches(filter.Field);

    public override int GetHashCode() => Field.GetHashCode();

    public override string ToString() => $"Reset Filter: {Field}";
}
 