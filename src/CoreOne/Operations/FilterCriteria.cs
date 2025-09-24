namespace CoreOne.Operations;

public class FilterCriteria(int type, string? desc, bool canRemove)
{
    public bool CanRemove { get; set; } = canRemove;
    public string Description { get; set; } = desc ?? string.Empty;
    public string? Field { get; init; }
    public string? FilterId { get; init; }
    public int Type { get; set; } = type;

    public override bool Equals(object? obj) => obj is FilterCriteria filter && Type == filter.Type;

    public override int GetHashCode() => Type.GetHashCode();
}
