using System.Diagnostics;
using System.Text;

namespace CoreOne.Operations;

[DebuggerDisplay("FilterBy: {Field} {Value}")]
public class FilterBy : IOperation
{
    //public AdvancedFilterContext? AdvancedSearch { get; set; }
    public string Field { get; }
    public string Id { get; }
    public string? Value { get; }

    public FilterBy()
    {
        Id = ID.Create().ToShortId();
        Field = string.Empty;
    }

    //public FilterBy(AdvancedFilterContext? search, string? field, string? value = null)
    //{
    //    Id = ID.Create().ToShortId();
    //    Field = field ?? string.Empty;
    //    AdvancedSearch = search;
    //    Value = value;
    //}

    public FilterBy(string value, string? field = null)
    {
        Id = ID.Create().ToShortId();
        Field = field ?? string.Empty;
        Value = value;
    }

    public override bool Equals(object? obj) => obj is FilterBy p && ToString().Matches(p.ToString());

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode($"{Field} {Value}");

    public override string ToString()
    {
        var builder = new StringBuilder(Field)
            .Append(' ');
        if (!string.IsNullOrEmpty(Value))
            builder.Append(Value);
        //if (AdvancedSearch is not null)
        //    builder.Append(AdvancedSearch);
        return builder.ToString();
    }
}