using CoreOne.Operations;
using System.Text;

namespace CoreOne.ODataBuilders;

public class ODataBuilder
{
    public const string
        SELECT = "$Select",
        TOP = "$Top",
        SKIP = "$Skip",
        ORDER = "$OrderBy",
        COUNT = "$Count",
        FILTER = "$Filter",
        EXPAND = "$Expand",
        ASC = "ASC",
        DESC = "DESC";

    private static readonly string[] Order = [COUNT, SKIP, TOP, SELECT, ORDER, EXPAND, FILTER];
    protected HashSet<FilterSegment> Filter { get; set; }
    protected HashSet<Segment> OrderBySegments { get; set; }
    protected Data<string, string> Parameters { get; set; }
    protected string RootUrl { get; set; }

    public ODataBuilder()
    {
        RootUrl = string.Empty;
        Filter = [];
        OrderBySegments = [];
        Parameters = new Data<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    [return: NotNullIfNotNull(nameof(builder))]
    public static implicit operator string?(ODataBuilder? builder) => builder?.ToString();

    public ODataBuilder Copy()
    {
        var builder = new ODataBuilder();
        builder.Url($"{RootUrl}");
        builder.OrderBySegments.AddRange(OrderBySegments);
        builder.Filter = [.. Filter];
        builder.Parameters = new Data<string, string>(Parameters, StringComparer.OrdinalIgnoreCase);
        return builder;
    }

    public ODataBuilder Count() => Set(COUNT, "true");

    public ODataBuilder Expand(string content)
    {
        var current = Parameters.Get(EXPAND, () => string.Empty);
        if (!string.IsNullOrEmpty(current) && !string.IsNullOrEmpty(content))
        {
            current += $",{content}";
        }
        else if (!string.IsNullOrWhiteSpace(content))
        {
            current = content;
        }

        Parameters.Set(EXPAND, current);
        return this;
    }

    public ODataBuilder FilterBy(string op, FormattableString value) => FilterBy(new FilterSegment(op, value));

    public ODataBuilder FilterBy(FilterSegment? filterSegment)
    {
        if (filterSegment.HasValue && (!string.IsNullOrEmpty(filterSegment.Value.Value) || filterSegment.Value.Count > 0))
            Filter.Add(filterSegment.Value);
        return this;
    }

    public ODataBuilder FilterByString(string op, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            FilterBy(new FilterSegment(op, $"{value}"));
        return this;
    }

    public bool HasKey(string key) => Parameters.ContainsKey(key);

    public ODataBuilder Merge(ODataBuilder builder)
    {
        if (builder is not null)
        {
            Filter.AddRange(builder.Filter);
            OrderBySegments.AddRange(builder.OrderBySegments);
            Select(builder.Parameters.Get(SELECT, () => string.Empty));
            builder.Parameters.Remove(SELECT);
            if (!string.IsNullOrEmpty(builder.RootUrl))
                RootUrl = builder.RootUrl;
            foreach (var kp in builder.Parameters)
            {
                Parameters.Set(kp.Key, kp.Value);
            }
        }
        return this;
    }

    public ODataBuilder OrderBy(OrderBy sort)
    {
        var dir = sort.Direction == SortDirection.Ascending ? ASC : DESC;
        OrderBySegments.Add(new Segment(dir, sort.Field));
        return this;
    }

    public ODataBuilder OrderBy(SortDirection order, string value)
    {
        var dir = order == SortDirection.Ascending ? ASC : DESC;
        OrderBySegments.Add(new Segment(dir, value));
        return this;
    }

    public ODataBuilder OrderBy(string order, string value)
    {
        OrderBySegments.Add(new Segment(order, value));
        return this;
    }

    public ODataBuilder Paginate(PageRequest? request) => request is not null && request.PageSize > 0 ?
            Skip((request.CurrentPage - 1) * request.PageSize).Top(request.PageSize) :
            this;

    public ODataBuilder RemoveFilter(Predicate<FilterSegment> predicate)
    {
        Filter.RemoveWhere(predicate);
        return this;
    }

    public ODataBuilder Select(string? select)
    {
        if (!string.IsNullOrEmpty(select))
        {
            var current = Parameters.Get(SELECT, () => "");
            if (!string.IsNullOrEmpty(current))
            {
                current += ",";
            }
            current += select;
            Parameters.Set(SELECT, current);
        }
        return this;
    }

    public ODataBuilder Skip(int skip = 0) => Set(SKIP, skip.ToString());

    public ODataBuilder Top(int top = 0) => Set(TOP, top.ToString());

    public override string ToString()
    {
        var builder = new StringBuilder();
        if (OrderBySegments.Count > 0)
        {
            Parameters.Set(ORDER, string.Join(",", OrderBySegments.Select(o => o.ToString())));
        }
        if (Filter.Count > 0)
        {
            var args = new StringBuilder();
            Filter.Each(filter => {
                if (args.Length > 0)
                    args.Append($" {filter.Operator} ");
                args.Append($"({filter})");
            });
            Parameters.Set(FILTER, args.ToString());
        }

        var segments = Parameters.OrderBy(p => Array.IndexOf(Order, p.Key))
               .ThenBy(p => p.Key)
               .SelectList(kp => $"{kp.Key}={kp.Value}");
        if (!string.IsNullOrEmpty(RootUrl))
        {
            builder.Append(RootUrl).Append('?');
        }
        builder.Append(string.Join("&", segments));
        return builder.ToString();
    }

    public ODataBuilder Url(string url)
    {
        RootUrl = url;
        return this;
    }

    protected ODataBuilder Set(string key, string value)
    {
        Parameters.Set(key, value);
        return this;
    }
}

#region -- Segments --

public struct FilterSegment : IEnumerable<FilterSegment>
{
    private readonly HashSet<FilterSegment> Segments;
    public readonly int Count => Segments.Count;
    public int FilterType { get; set; }
    public string? Operator { get; }
    public string Value { get; }

    public FilterSegment(BinaryOperator op) : this(op.ToString())
    {
    }

    public FilterSegment(string? op)
    {
        Value = "";
        FilterType = 0;
        Operator = op?.ToUpper();
        Segments = [];
    }

    public FilterSegment(BinaryOperator op, FormattableString? value) : this(op.ToString(), value)
    {
    }

    public FilterSegment(string? op, FormattableString? value)
    {
        FilterType = 0;
        Operator = op?.ToUpper();
        Segments = [];
        Value = PrepareValue(value);
    }

    public FilterSegment(int filterType, string op, FormattableString? value)
    {
        FilterType = filterType;
        Operator = op?.ToUpper();
        Segments = [];
        Value = PrepareValue(value);
    }

    public static FilterSegment? Create<T>(IEnumerable<T>? items, string op, string fieldName)
    {
        var array = items?.ToArray();
        if (array?.Length > 0)
        {
            var type = typeof(T);
            var clause = array.Length == 1 ? "eq" : "in";
            var tostring = ODataOperator.GetToString(type);
            var stritems = array.Length == 1 ? tostring?.Invoke(array[0]) : $"({string.Join(",", array.Select(p => tostring?.Invoke(p)))})";
            return new FilterSegment(op, $"{fieldName} {clause} {stritems}");
        }
        return null;
    }

    public static FilterSegment? Create<T>(IEnumerable<T>? items, BinaryOperator op, string fieldName) => Create(items, op.ToString(), fieldName);

    public readonly void Add(FilterSegment? segment)
    {
        if (segment.HasValue)
            Segments.Add(segment.Value);
    }

    public readonly void Add(BinaryOperator op, FormattableString value) => Segments.Add(new FilterSegment(op, value));

    public readonly void Add(string op, FormattableString value) => Segments.Add(new FilterSegment(op, value));

    public readonly void AddRange(IEnumerable<FilterSegment> segments)
    {
        if (segments is not null)
        {
            foreach (var p in segments)
                Segments.Add(p);
        }
    }

    public readonly void AddString(string op, string value) => Segments.Add(new FilterSegment(op, $"{value}"));

    public readonly IEnumerator<FilterSegment> GetEnumerator() => Segments.GetEnumerator();

    readonly IEnumerator IEnumerable.GetEnumerator() => Segments.GetEnumerator();

    public override readonly int GetHashCode() => Segments.Count == 0 ? (Operator, Value).GetHashCode() : (Operator, Value, Segments).GetHashCode();

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(Value ?? "");
        if (Segments.Count > 0)
        {
            foreach (var f in Segments)
            {
                if (builder.Length > 0)
                    builder.Append($" {f.Operator} ");
                builder.Append($"({f})");
            }
        }
        return builder.ToString();
    }

    private static string PrepareValue(FormattableString? template)
    {
        if (template == null)
            return string.Empty;

        var encodedArgs = new object[template.ArgumentCount];
        for (var i = 0; i < template.ArgumentCount; i++)
            encodedArgs[i] = new ODataArgument(template.GetArgument(i));

        return string.Format(template.Format, encodedArgs);
    }
}

public readonly struct Segment(string op, string value)
{
    public string Operator { get; } = op;
    public string Value { get; } = value;

    public static bool operator !=(Segment left, Segment right) => !(left == right);

    public static bool operator ==(Segment left, Segment right) => left.Equals(right);

    public override bool Equals(object? obj) => obj is Segment p && ToString().Matches(p.ToString());

    public override int GetHashCode() => ToString().GetHashCode();

    public override string ToString() => $"{Value} {Operator}";
}

#endregion -- Segments --