namespace CoreOne.Operations;

public class MergeFilter(string? field, Func<FilterBy?, FilterBy> onmerge) : IOperation
{
    private readonly Func<FilterBy?, FilterBy> OnMerge = onmerge;
    public string Field { get; } = field ?? string.Empty;

    public bool Apply(List<IOperation> operations)
    {
        var current = operations.OfType<FilterBy>().FirstOrDefault(p => p.Field.Matches(Field));
        current = Merge(current);
        operations.AddOrUpdate(current, p => p is FilterBy fb && fb.Field.Matches(current.Field));
        return true;
    }

    public FilterBy Merge(FilterBy? current) => OnMerge.Invoke(current);
}
