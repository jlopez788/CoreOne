using CoreOne;
using CoreOne.Lookups;
using CoreOne.ODataBuilders;

namespace CoreOne.ODataBuilders;

public class ODataOperator : LookupType<ODataOperator>
{
    #region Operators

    private class BetweenOperator : ODataOperator
    {
        public BetweenOperator() : base("bt", "Between", null)
        {
        }

        public override bool CanUseOperatorOnType(Type? type) => false;

        public override FilterSegment? GetODataFilter(FilterContext context)
        {
            if (context is not null)
            {
                if (context.FilterResult.Value is not null)
                    throw new InvalidOperationException($"Between operator is not supported for {nameof(FilterContext.FilterResult)} property. Please use {nameof(FilterContext.AdvancedSearch)}");
                if (context.AdvancedSearch is not null)
                {
                    var type1 = context.AdvancedSearch.Value1?.GetType();
                    var type2 = context.AdvancedSearch.Value2?.GetType();
                    if (type1 == Types.String && type2 == Types.String)
                    {
                        var first = context.AdvancedSearch.Value1!.ToString()!;
                        var last = context.AdvancedSearch.Value2!.ToString()!;
                        var letterRange = Enumerable.Range('a', 'z' - 'a' + 1).Where(c => c >= first[0] && c <= last[0]).Select(c => (char)c);
                        var segment = new FilterSegment(context.SuggestedOperator);
                        foreach (var letter in letterRange)
                        {
                            string keyword = letter.ToString();
                            if (letter == first[0])
                                keyword = first;
                            else if (letter == last[0])
                                keyword = last;
                            segment.Add("OR", $"startswith({context.FieldName},'{keyword}')");
                        }
                    }
                    else
                    {
                        FilterSegment? segment1 = context.AdvancedSearch.Value1 is not null ? new FilterSegment("AND", $"{context.FieldName} ge {context.AdvancedSearch.Value1:odata}") : null;
                        FilterSegment? segment2 = context.AdvancedSearch.Value2 is not null ? new FilterSegment("AND", $"{context.FieldName} le {context.AdvancedSearch.Value2:odata}") : null;
                        return new FilterSegment(context.SuggestedOperator) { segment1, segment2 };
                    }
                }
            }
            return null;
        }
    }

    private class ContainsOperator : ODataOperator
    {
        public ContainsOperator() : base("ct", "Contains", "fas fa-asterisk")
        {
            IsDefault = true;
        }

        public override bool CanUseOperatorOnType(Type? type) => type == Types.String;

        public override FilterSegment? GetODataFilter(FilterContext context)
        {
            if (context is not null)
            {
                var value = context.FilterResult.Value ?? context.AdvancedSearch?.Value1;
                if (value is not null)
                    return new FilterSegment(context.SuggestedOperator, $"contains({context.FieldName}, {value:odata})");
            }
            return null;
        }
    }

    private class DefaultOperator : ODataOperator
    {
        private static readonly Lazy<Data<Type, CreateSegment>> Data = new(InitializeData);

        public DefaultOperator() : base("df", "Default", "fas fa-asterisk") { }

        public override bool CanUseOperatorOnType(Type? type) => false;

        public override FilterSegment? GetODataFilter(FilterContext context) => Data.Value.Get(context.Field?.Type)?.Invoke(context);

        private static Data<Type, CreateSegment> InitializeData()
        {
            var parsers = Parsers.Value;
            var equalFilter = new CreateSegment(c => new FilterSegment(c.SuggestedOperator, $"{c.FieldName} eq {get(c)}"));
            var data = new Data<Type, CreateSegment>()
               .SetDefaultKey(Types.Void);
            data.Set(Types.Void, equalFilter);
            data.Set(Types.Bool, c => new FilterSegment(c.SuggestedOperator, $"{c.FieldName} eq {get(c)}"));
            data.Set(Types.String, c => new FilterSegment(c.SuggestedOperator, $"contains({c.FieldName}, {get(c)})"));
            data.Set(Types.DateTime, c => new FilterSegment(c.SuggestedOperator, $"{c.FieldName} eq {get(c)}"));

            string? get(FilterContext context) => parsers.Get(context.Field?.Type)?.Invoke(context.FilterResult.Value);
            return data;
        }
    }

    private class EqualOperator : ODataOperator
    {
        public EqualOperator() : base("eq", "Equals", "fas fa-equals")
        {
            IsDefault = true;
        }

        public override bool CanUseOperatorOnType(Type? type) => true;
    }

    private class GreaterThanOperator : ODataOperator
    {
        public GreaterThanOperator() : base("ge", "Greater Than", "fas fa-greater-than-equal") { }

        public override bool CanUseOperatorOnType(Type? type) => type != Types.Guid && type != Types.NGuid;
    }

    private class InArrayOperator : ODataOperator
    {
        private readonly MethodInfo? Create;

        public InArrayOperator() : base("in", "Contains", "fas fa-asterisk")
        {
            var methods = typeof(FilterSegment).GetMethods(BindingFlags.Static | BindingFlags.Public);
            Create = methods.FirstOrDefault(p => {
                var args = p.GetParameters();
                return p.Name == nameof(FilterSegment.Create) && args?.Length == 3 && args[1].ParameterType == Types.String;
            });
        }

        public override bool CanUseOperatorOnType(Type? type) => false;

        public override FilterSegment? GetODataFilter(FilterContext context)
        {
            if (context is not null)
            {
                if (context.FilterResult.Value is not null)
                    throw new InvalidOperationException($"InArray operator is not supported for {nameof(FilterContext.FilterResult)} property. Please use {nameof(FilterContext.AdvancedSearch)}");
                if (context.AdvancedSearch?.Value1 is not null)
                {
                    var type = context.AdvancedSearch?.Value1.GetType();
                    if (type?.IsGenericType == true)
                    {
                        var args = type.GetGenericArguments();
                        if (args.Length > 0)
                        {
                            type = args[0];
                            var method = Create?.MakeGenericMethod(type);
                            var segment = (FilterSegment?)method?.Invoke(null, [context.AdvancedSearch?.Value1, "AND", context.FieldName]);
                            return segment;
                        }
                    }
                }
            }
            return null;
        }
    }

    private class LessThanOperator : ODataOperator
    {
        public LessThanOperator() : base("le", "Less Than", "fas fa-less-than-equal") { }

        public override bool CanUseOperatorOnType(Type? type) => type != Types.Guid && type != Types.NGuid;
    }

    private class NotNullOperator : ODataOperator
    {
        public NotNullOperator() : base("ne", "Is Not Empty", "fas fa-star")
        {
            RequiresInput = false;
        }

        public override FilterSegment? GetODataFilter(FilterContext context)
        {
            var type = context.Field?.Type;
            var filter = new FilterSegment(context.SuggestedOperator) {
                { "OR", $"{context.FieldName} ne null" }
            };
            if (type == Types.String)
                filter.Add("OR", $"{context.FieldName} ne ''");
            return filter;
        }
    }

    private class NullOrEmptyOperator : ODataOperator
    {
        public NullOrEmptyOperator() : base("null", "Is Empty", "far fa-star")
        {
            RequiresInput = false;
        }

        public override FilterSegment? GetODataFilter(FilterContext context)
        {
            var type = context.Field?.Type;
            var filter = new FilterSegment(context.SuggestedOperator) {
                { "OR", $"{context.FieldName} eq null" }
            };
            if (type == Types.String)
                filter.Add("OR", $"{context.FieldName} eq ''");
            return filter;
        }
    }

    private class StartsWithOperator : ODataOperator
    {
        public StartsWithOperator() : base("st", "Starts With", "fas fa-star-half-alt")
        {
        }

        public override bool CanUseOperatorOnType(Type? type) => type == Types.String;

        public override FilterSegment? GetODataFilter(FilterContext context)
        {
            if (context is not null)
            {
                var value = context.FilterResult.Value ?? context.AdvancedSearch?.Value1;
                if (value is not null)
                    return new FilterSegment(context.SuggestedOperator, $"startswith({context.FieldName}, {value:odata})");
            }
            return null;
        }
    }

    #endregion Operators

    internal delegate string ParseToString(object? value);

    public static readonly ODataOperator
        Default = new DefaultOperator(),
        Contains = new ContainsOperator(),
        Equal = new EqualOperator(),
        GreaterThan = new GreaterThanOperator(),
        LessThan = new LessThanOperator(),
        Between = new BetweenOperator(),
        InArray = new InArrayOperator(),
        StartsWith = new StartsWithOperator(),
        NullOrEmpty = new NullOrEmptyOperator(),
        NotNull = new NotNullOperator();

    internal static readonly Lazy<Data<Type, ParseToString>> Parsers = new(InitializeParsers);
    public string? IconCssClass { get; init; }
    public bool IsDefault { get; init; }
    public bool? RequiresInput { get; init; }

    protected ODataOperator()
    {
    }

    protected ODataOperator(string code, string description, string? iconCss) : base(code, description)
    {
        IconCssClass = iconCss;
    }

    public static bool operator !=(ODataOperator? x, ODataOperator? y) => !ReferenceEqualityComparer<ODataOperator>.Default.Equals(x, y);

    public static bool operator ==(ODataOperator? x, ODataOperator? y) => ReferenceEqualityComparer<ODataOperator>.Default.Equals(x, y);

    public virtual bool CanUseOperatorOnType(Type? type) => type is not null && !type.IsEnum;

    public override bool Equals(object? obj) => obj is ODataOperator o && Code.Matches(o.Code);

    public override int GetHashCode() => Code?.GetHashCode() ?? 0;

    public virtual FilterSegment? GetODataFilter(FilterContext context)
    {
        if (context is not null)
        {
            var value = context.FilterResult.Value ?? context.AdvancedSearch?.Value1;
            if (value is not null)
                return new FilterSegment(context.SuggestedOperator, $"{context.FieldName} {Code} {value:odata}");
        }

        return null;
    }

    public override string ToString() => $"{Description} ({Code})";

    internal static ParseToString? GetToString(Type? type)
    {
        type = type is not null ? Nullable.GetUnderlyingType(type) ?? type : Types.Void;
        if (type?.IsEnum == true)
            type = Types.String;
        return Parsers.Value.Get(type ?? Types.Void);
    }

    private static string DateTimeToString(object? odt) => odt is DateTime dt ? dt.TimeOfDay == TimeSpan.Zero ? $"{dt:yyyy-MM-dd}" : $"{dt:yyyy-MM-ddTHH:mm:ss}" : "null";

    private static Data<Type, ParseToString> InitializeParsers() => new Data<Type, ParseToString>()
        .SetDefaultKey(Types.Void)
        .Set(Types.String, ObjectStringToString)
        .Set(Types.DateTime, DateTimeToString)
        .Set(Types.Void, o => o?.ToString() ?? "null")
        .Set(Types.Bool, o => o?.ToString()?.ToLower() ?? "null");

    private static string ObjectStringToString(object? odt)
    {
        var value = odt?.ToString();
        value = value is not null ? Utility.EncodeForUrl(value.Replace("'", "''")) : "";
        return $"'{value}'";
    }
}

public class ODataOperatorCollection : List<ODataOperator>
{
}