namespace CoreOne.Operations;

public class MergeFilter(string? field, Func<FilterBy?, FilterBy> onmerge) : IOperation
{
    private readonly Func<FilterBy?, FilterBy> OnMerge = onmerge;
    public string Field { get; } = field ?? string.Empty;

    public FilterBy Merge(FilterBy? current) => OnMerge.Invoke(current);
}
