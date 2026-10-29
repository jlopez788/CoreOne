using System.Diagnostics;

namespace CoreOne.Operations;

[DebuggerDisplay("SingleOrderBy: {Field} {Direction}")]
public class SingleOrderBy : OrderBy
{
    public SingleOrderBy() => Field = string.Empty;

    public SingleOrderBy(string field, string direction)
    {
        Field = field ?? string.Empty;
        Direction = direction.MatchesAny("Ascending", "ASC") ? SortDirection.Ascending : SortDirection.Descending;
    }

    public SingleOrderBy(string field, SortDirection direction)
    {
        Field = field ?? string.Empty;
        Direction = direction;
    }

    public override bool Apply(List<IOperation> operations)
    {
        operations.RemoveAll(p => p is OrderBy);
        operations.Add(this);
        return true;
    }

    public override bool Equals(object? obj) => obj is SingleOrderBy p && ToString().Matches(p.ToString());

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode($"{Field} {Direction}");

    public override string ToString() => $"{Field} {Direction}";
}