using CoreOne.Operations;

namespace CoreOne.ODataBuilders;

public class ODataPageRequestBuilder(PageRequest request, ODataBuilder? builder = null)
{
    protected ODataBuilder Builder { get; private set; } = builder ?? new ODataBuilder();
    protected FilterTypeHandlerFactory Handler { get; } = FilterTypeHandlerFactory.Default.Clone();
    protected Data<string, InterceptSegment> Intercept { get; } = new Data<string, InterceptSegment>(StringComparer.OrdinalIgnoreCase);
    protected PageRequest Request { get; } = request;

    public ODataBuilder GetBuilder() => Builder;

    public ODataPageRequestBuilder UseFilters()
    {
        var filters = Request.GetFilters().ToArray();
        if (filters == null || filters.Length == 0)
            return this;

        IEnumerable<FilterSegment?>? segments = null;
        var fields = Request.GetAvailableFields().ToArray();
        var filter = filters.FirstOrDefault(p => string.IsNullOrEmpty(p.Field));
        if (!string.IsNullOrEmpty(filter?.Value))
        {
            var mappedFields = fields
                .GroupBy(p => getTypeKey(p.Type), p => p, (k, g) => new { k, g })
                .ToDictionary(p => p.k ?? Types.Void, p =>
#if NET9_0_OR_GREATER
                    p.g.ToHashSet()
#else
                    p.g.ToList()
#endif
                    );
            mappedFields.Remove(Types.Void); // We have no idea how to process these types. Do they even exist in the model?
            segments = Handler.Where(p => mappedFields.ContainsKey(p.Key))
                    .Select(p => getSegments(p.Value))
                    .SelectMany(p => p);

            Type getTypeKey(Type type) => type is not null ? Nullable.GetUnderlyingType(type) ?? type : Types.Void;
            IEnumerable<FilterSegment?> getSegments(IFilterTypeHandler? handler) => handler?.Type is not null ? mappedFields[handler.Type].Select(field => getFilter(handler, field, BinaryOperator.Or, filter)) : [];
        }
        else
        {
            segments = filters.Select(p => {
                var field = fields.FirstOrDefault(f => f.Field.Matches(p.Field));
                if (field is not null && Handler.TryGetValue(field.Type, out var handler) && handler is not null)
                    return getFilter(handler, field, BinaryOperator.And, p);
                else
                {
                    var type = field?.Type;
                    var isenum = type?.IsEnum == true;
                    if (type?.IsGenericType == true)
                    {
                        var args = type.GetGenericArguments();
                        if (args.Length > 0)
                        {
                            type = args[0];
                            isenum = type.IsEnum;
                        }
                    }
                    if (isenum && field is not null)
                    {
                        handler = new FilterTypeHandler(type!, p => FilterTypeResult.Empty, c => c.GetODataFilter());
                        return getFilter(handler, field, BinaryOperator.And, p);
                    }
                }

                return null;
            });
        }

        if (segments is not null)
        {
            var segment = new FilterSegment(BinaryOperator.And);
            segment.AddRange(segments.ExcludeNulls());
            Builder.FilterBy(segment);
        }

        return this;

        FilterSegment? getFilter(IFilterTypeHandler handler, AvailableField field, BinaryOperator op, FilterBy filter)
        {
            var method = new CreateSegment(context => {
                var intercept = Intercept.Get(field.Field);
                return intercept is not null ? intercept.Invoke(context, handler.CreateSegment) : handler.CreateSegment(context);
            });
            if (!string.IsNullOrEmpty(filter.Value))
            {
                var result = handler.Parse(filter.Value);
                var fcontext = new FilterContext(field, result, op) {
                    AdvancedSearch = filter.AdvancedSearch
                };
                return result.Parsed || Intercept.ContainsKey(field.Field) ? method(fcontext) : null;
            }
            else if (filter.AdvancedSearch is not null)
            {
                return method(new FilterContext(field, FilterTypeResult.Empty, op) {
                    AdvancedSearch = filter.AdvancedSearch
                });
            }
            return null;
        }
    }

    public ODataPageRequestBuilder UseHandler(IFilterTypeHandler handler)
    {
        if (handler?.Type is not null)
            Handler.Set(handler.Type, handler);
        return this;
    }

    public ODataPageRequestBuilder UseInterceptOnField(string field, InterceptSegment interceptSegment)
    {
        Intercept.Set(field, interceptSegment);
        return this;
    }

    public ODataPageRequestBuilder UsePagination()
    {
        Builder.Count()
            .Paginate(Request);
        return this;
    }

    public ODataPageRequestBuilder UseSort(OrderBy? defaultSort = null)
    {
        var sort = Request.GetSortFields();
        if (sort.Any())
            sort.Each(p => addSort(p));
        else if (defaultSort is not null)
            addSort(defaultSort);
        return this;

        void addSort(OrderBy p) => Builder.OrderBy(p.Direction == SortDirection.Ascending ? ODataBuilder.ASC : ODataBuilder.DESC, p.Field);
    }
}

public delegate FilterSegment? CreateSegment(FilterContext context);

public delegate FilterSegment? InterceptSegment(FilterContext context, CreateSegment createSegment);