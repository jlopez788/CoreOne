using System.Diagnostics;

namespace OneCore.Operations;

[DebuggerDisplay("OrderBy: {Field} {Direction}")]
public class OrderBy : IOperation
{
    public SortDirection Direction { get; }
    public string Field { get; }

    public OrderBy(string field, string direction)
    {
        Field = field ?? string.Empty;
        Direction = direction.MatchesAny("Ascending", "ASC") ? SortDirection.Ascending : SortDirection.Descending;
    }

    public OrderBy(string field, SortDirection direction)
    {
        Field = field ?? string.Empty;
        Direction = direction;
    }

    public static OrderBy Ascending(string field) => new(field, SortDirection.Ascending);

    public static OrderBy Descending(string field) => new(field, SortDirection.Descending);

    public override bool Equals(object? obj) => obj is OrderBy p && ToString().Matches(p.ToString());

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode($"{Field} {Direction}");

    public override string ToString() => $"{Field} {Direction}";
}