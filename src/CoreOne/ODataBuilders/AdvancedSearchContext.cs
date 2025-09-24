using System.Text;

namespace CoreOne.ODataBuilders;

public class AdvancedFilterContext
{
    public ODataOperator Operator { get; init; }
    public object? Value1 { get; init; }
    public object? Value2 { get; init; }

    public AdvancedFilterContext() => Operator = ODataOperator.Default;

    public AdvancedFilterContext(ODataOperator @operator, object? value1 = null, object? value2 = null)
    {
        Operator = @operator;
        Value1 = value1;
        Value2 = value2;
    }

    public override string ToString()
    {
        var addedOne = false;
        var builder = new StringBuilder()
            .Append(Operator.Description);

        if (Value1 is not null)
        {
            var sval = MapString(Value1);
            if (!string.IsNullOrEmpty(sval))
            {
                addedOne = true;
                builder.Append(' ')
                    .Append(sval);
            }
        }
        if (Value2 is not null)
        {
            var sval = MapString(Value2);
            if (!string.IsNullOrEmpty(sval))
            {
                builder.Append(addedOne ? " - " : " ")
                    .Append(sval);
            }
        }

        return builder.ToString();

        static string? MapString(object? value) => value is null ? null : value switch {
            bool flag => flag ? "YES" : "NO",
            string val => val,
            DateTime dt => dt.TimeOfDay == TimeSpan.Zero ? dt.ToString("MM/dd/yyyy") : dt.ToString("MM/dd/yyyy HH:mm tt"),
            IEnumerable items => MapItems(items),
            _ => value.ToString()
        };
        static string? MapItems(IEnumerable? items)
        {
            if (items is null)
                return null;

            var values = new List<string>();
            foreach (var item in items)
            {
                var sval = MapString(item);
                if (!string.IsNullOrEmpty(sval))
                    values.Add(sval);
            }
            return string.Join(", ", values);
        }
    }
}