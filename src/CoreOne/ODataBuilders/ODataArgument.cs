
namespace CoreOne.ODataBuilders;

internal class ODataArgument(object? value) : IFormattable
{
    public object? Value { get; } = value;

    public string ToString(string? format, IFormatProvider? formatProvider) => format?.ToLower() switch {
        "odata" => Utility.AsODataUrlValue(Value),
        string p when p is not null && Value is not null => Format(Value, format) ?? string.Empty,
        _ => Value?.ToString() ?? string.Empty
    };

    private static string? Format(object value, string format)
    {
        var type = value.GetType();
        type = Nullable.GetUnderlyingType(type) ?? type;
        var tostring = MetaType.GetInvokeMethod(type, nameof(ToString), Types.String);
        return tostring.Invoke(value, [format])?.ToString();
    }
}