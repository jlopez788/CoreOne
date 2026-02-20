using CoreOne.Threading.Tasks;
using System.Text;

namespace CoreOne.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Returns a value indicating whether a specified substring occurs within this string.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="other"></param>
    /// <param name="comparison"></param>
    /// <returns></returns>
    public static bool ContainsX(this string? value, string? other, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        return !string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(other) &&
#if NET9_0_OR_GREATER
            value.Contains(other, comparison);
#else
            value!.ToLower().Contains(other!.ToLower());
#endif
    }

    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value) => string.IsNullOrWhiteSpace(value);

    public static bool IsNotNullOrEmpty([NotNullWhen(true)] this string? value) => !string.IsNullOrWhiteSpace(value);

    public static string EndWith(this string? value, string endwith) => !string.IsNullOrEmpty(value) ? (value!.EndsWith(endwith) ? value : $"{value}{endwith}") : endwith;

    /// <summary>
    /// Compare if one string equals another string, case-insensitive by default
    /// </summary>
    /// <param name="value"></param>
    /// <param name="other"></param>
    /// <param name="comparison"></param>
    /// <returns></returns>
    public static bool Matches(this string? value, string? other, StringComparison comparison = StringComparison.OrdinalIgnoreCase) => string.Compare(value, other, comparison) == 0;

    /// <summary>
    /// Compares if the given string exists in any of the provided list of strings, case-insensitive.
    /// </summary>
    /// <param name="value">Target value to find in the list.</param>
    /// <param name="args">List of strings to compare against.</param>
    /// <returns>True if the word is found in the list.</returns>
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
    public static string Remove(this string? value, params char[] remove)
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

    public static IEnumerable<string> SplitBy(this string? value, char[] separator)
    {
        return value?.Split(separator)
            .Select(p => p.Trim())
            .ExcludeNullOrEmpty() ?? [];
    }

    public static string Separate(this string input, string separator)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrEmpty(input))
        {
            bool islastupper = false;
            char[] chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsUpper(chars[i]))
                {
                    if (!islastupper)
                    {
                        if (i > 0)
                            builder.Append(separator);
                        builder.Append(chars[i]);
                    }
                    else
                    {
                        int idx = i + 1;
                        if ((idx < chars.Length) && !char.IsUpper(chars[idx]) && char.IsLetterOrDigit(chars[idx]))
                            builder.Append(separator);
                        builder.Append(chars[i]);
                    }
                    islastupper = true;
                }
                else
                {
                    islastupper = false;
                    builder.Append(chars[i]);
                }
            }
        }
        return builder.ToString().ToLowerInvariant().Trim();
    }
}