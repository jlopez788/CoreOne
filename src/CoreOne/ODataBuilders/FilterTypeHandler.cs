namespace CoreOne.ODataBuilders;

public interface IFilterTypeHandler
{
    Type Type { get; }

    FilterSegment? CreateSegment(FilterContext context);

    FilterTypeResult Parse(string? value);
}

public class FilterTypeHandler(Type type, Func<string?, FilterTypeResult> parse, CreateSegment createSegment) : IFilterTypeHandler
{
    private readonly int Hash = type.GetHashCode();
    private readonly CreateSegment OnCreateSegment = createSegment;
    private readonly Func<string?, FilterTypeResult> OnParse = parse;
    public Type Type { get; } = type;

    public FilterSegment? CreateSegment(FilterContext context) => OnCreateSegment.Invoke(context);

    public override bool Equals(object? obj) => obj is FilterTypeHandler p && Type is not null && Type == p.Type;

    public override int GetHashCode() => Hash;

    public FilterTypeResult Parse(string? value) => OnParse.Invoke(value);
}