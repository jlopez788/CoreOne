namespace CoreOne.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Compare if one string equals another string, case-insensitive by default
    /// </summary>
    /// <param name="value"></param>
    /// <param name="other"></param>
    /// <param name="comparison"></param>
    /// <returns></returns>
    public static bool Matches(this string? value, string? other, StringComparison comparison = StringComparison.OrdinalIgnoreCase) => string.Compare(value, other, comparison) == 0;

    /// <summary>
    /// Compare if given string exists in any of the provided list of strings, case-insensitive
    /// </summary>
    /// <param name="value">Target value to find in <param name="args"/>
    /// <param name="args"></param>
    /// <returns>True if word is found in list</returns>
    public static bool MatchesAny(this string? value, params string[] args) => MatchesAny(value, StringComparison.OrdinalIgnoreCase, args);

    /// <summary>
    /// Compare if given string exists in any of the provided list of strings
    /// </summary>
    /// <param name="value"></param>
    /// <param name="comparison"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static bool MatchesAny(this string? value, StringComparison comparison, params string[] args) => !string.IsNullOrEmpty(value) && args.Any(s => s.Matches(value, comparison));

    /// <summary>
    /// Remove characters from given string
    /// </summary>
    /// <param name="value"></param>
    /// <param name="remove"></param>
    /// <returns></returns>
    public static string Remove(this string value, params char[] remove)
    {
        value ??= "";
        if (!string.IsNullOrEmpty(value))
        {
            var chars = value.Where(c => Array.BinarySearch(remove, c) < 0);
            value = new string([.. chars]);
        }
        return value;
    }

    public static string? ToXString<T>(this T? model, string? format = null, bool usedefault = true)
    {
        string formatted = "";
        InvokeCallback method;
        var type = typeof(T);
        object? value = model;
        if (usedefault && (type != null))
        {
            object? alt = type.GetDefault();
            if (type.IsNullable())
            {
                Type? underlying = Nullable.GetUnderlyingType(type);
                if (underlying is not null)
                {
                    method = MetaType.GetInvokeMethod(type, "GetValueOrDefault", underlying);
                    type = underlying;
                    value = method.Invoke(model, [alt]);
                }
            }
            else if (alt is not null)
                value ??= alt;
        }

        if (!string.IsNullOrEmpty(format))
        {
            if ((type != null) && type.IsNullable())
                type = Nullable.GetUnderlyingType(type);
            method = MetaType.GetInvokeMethod(type, "ToString", Types.String);

            try
            {
                object? temp = method.Invoke(model, [format]);
                value = temp;
            }
            catch { }
        }
        if (value != null)
            formatted = value?.ToString() ?? string.Empty;
        return formatted;
    }
}