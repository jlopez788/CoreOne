using CoreOne.Operations;

namespace CoreOne.ODataBuilders;

public class FilterContext
{
    public AdvancedFilterContext? AdvancedSearch { get; set; }
    public AvailableField? Field { get; set; }
    public string? FieldName => Field?.Field;
    public FilterTypeResult FilterResult { get; set; }
    public BinaryOperator SuggestedOperator { get; set; }

    public FilterContext(AvailableField field, FilterTypeResult filterResult, BinaryOperator op)
    {
        Field = field;
        FilterResult = filterResult;
        SuggestedOperator = op;
    }

    public FilterContext(AvailableField? field, FilterTypeResult filterResult)
    {
        Field = field;
        FilterResult = filterResult;
    }

    public FilterSegment? GetODataFilter() => AdvancedSearch is not null ? AdvancedSearch.Operator.GetODataFilter(this) : ODataOperator.Default.GetODataFilter(this);

    public FilterContext Next(AdvancedFilterContext? search)
    {
        return new FilterContext(Field, FilterResult) {
            SuggestedOperator = SuggestedOperator,
            AdvancedSearch = search
        };
    }
}