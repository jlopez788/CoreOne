using CoreOne.ODataBuilders;
using CoreOne.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Web;

namespace CoreOne;

public static partial class Utility
{
    public static string AsODataUrlValue<T>(this T value)
    {
        var type = typeof(T);
        if (type == Types.Object)
            type = value?.GetType();
        var tostring = ODataOperator.GetToString(type);
        return tostring?.Invoke(value) ?? string.Empty;
    }

    /// <summary>
    /// Format phone number
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <param name="mask">Masks all except last 4 digts</param>
    /// <returns></returns>
    public static string FormatPhoneNumber(string? phoneNumber, bool mask = false)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return string.Empty;

        var regex = PhoneReg();
        phoneNumber = regex.Replace(phoneNumber, string.Empty);
        if (mask)
        {
#if NET9_0_OR_GREATER
            return phoneNumber.Length switch {
                7 => "***-" + phoneNumber[^4..],
                10 => "(***) ***-" + phoneNumber[^4..],
                11 or 12 or 13 => code() + " (***) ***-" + phoneNumber[^4..],
                _ => "".PadRight(phoneNumber.Length - 4, '*') + phoneNumber[^4..],
            };
#else
            return phoneNumber.Length switch {
                7 => "***-" + phoneNumber.Substring(phoneNumber.Length - 4),
                10 => "(***) ***-" + phoneNumber.Substring(phoneNumber.Length - 4),
                11 or 12 or 13 => code() + " (***) ***-" + phoneNumber.Substring(phoneNumber.Length - 4),
                _ => "".PadRight(phoneNumber.Length - 4, '*') + phoneNumber.Substring(phoneNumber.Length - 4),
            };
#endif
        }

        var (reg, format) = phoneNumber.Length switch {
            7 => (reg: PhoneSevenReg(), format: "$1-$2"),
            10 => (reg: PhoneTenReg(), format: "($1) $2-$3"),
            11 or 12 or 13 => (reg: PhonePlusReg(), format: "+$1 ($2) $3-$4"),
            _ => (reg: null, format: null)
        };
        return reg?.Replace(phoneNumber, format!)?.Replace("++", "+") ?? phoneNumber;

#if NET9_0_OR_GREATER
        string code() => "+" + phoneNumber![..^10];
#else
        string code() => "+" + phoneNumber.Substring(0, phoneNumber.Length - 10);
#endif
    }

    /// <summary>
    /// Safe await a nullable task
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static Task SafeAwait(Task? callback) => callback is not null ? callback : Task.CompletedTask;

#if NET9_0_OR_GREATER

    /// <summary>
    /// Safe await a nullable task
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static ValueTask SafeAwait(ValueTask? callback) => callback.HasValue ? callback.Value : ValueTask.CompletedTask;

#endif

    /// <summary>
    /// Safe await a nullable task
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static async Task<T?> SafeAwait<T>(Task<T>? callback) => callback is not null ? await callback : default;

    /// <summary>
    /// Safe await a nullable task
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    public static async ValueTask SafeAwait(SafeTask? task)
    {
        if (task is not null)
            await task;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static IResult Try(Action? callback)
    {
        try
        { callback?.Invoke(); }
        catch (Exception ex) { return Result.FromException(ex); }
        return Result.Ok;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static async Task<IResult> Try(Func<Task>? callback)
    {
        try
        { await SafeAwait(callback?.Invoke()); }
        catch (Exception ex) { return Result.FromException(ex); }
        return Result.Ok;
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static async Task<IResult<T>> Try<T>(Func<Task<T?>>? callback)
    {
        try
        {
            var model = callback is not null ? await callback.Invoke() : default;
            return new Result<T>(model);
        }
        catch (Exception ex) { return Result.FromException<T>(ex); }
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static IResult<T> Try<T>(Func<T?>? callback)
    {
        try
        {
            var model = callback is not null ? callback.Invoke() : default;
            return new Result<T>(model);
        }
        catch (Exception ex) { return Result.FromException<T>(ex); }
    }

    public static bool TryChangeType<TValue>(object? value, [NotNullWhen(true)] out TValue? result, CultureInfo? cultureInfo = null)
    {
        try
        {
            var svalue = value?.ToString() ?? "";
            Type conversionType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
            result =
#if NET9_0_OR_GREATER
                  conversionType.IsEnum && Types.TryParseEnum<TValue>(value?.ToString(), conversionType, out var theEnum)
                  ? theEnum! :
#endif
                 conversionType == typeof(Guid)
                ? (TValue)Convert.ChangeType(Guid.Parse(svalue), conversionType)
                : conversionType == typeof(DateTimeOffset)
                ? (TValue)Convert.ChangeType(DateTimeOffset.Parse(svalue), conversionType)
                : Convert.ChangeType(value, conversionType, cultureInfo ?? CultureInfo.InvariantCulture) is TValue t ? t :
                Types.Parse<TValue>(value).Model;

            return result is not null;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    public static string EncodeForUrl(string value) => HttpUtility.UrlEncode(value);

#if NET9_0_OR_GREATER

    [GeneratedRegex(@"(\d{1,3})(\d{3})(\d{3})(\d{4})")]
    private static partial Regex PhonePlusReg();

    [GeneratedRegex(@"[^\d+]")]
    private static partial Regex PhoneReg();

    [GeneratedRegex(@"(\d{3})(\d{4})")]
    private static partial Regex PhoneSevenReg();

    [GeneratedRegex(@"(\d{3})(\d{3})(\d{4})")]
    private static partial Regex PhoneTenReg();

#else

    private static Regex PhonePlusReg() => new Regex(@"(\d{1,3})(\d{3})(\d{3})(\d{4})", RegexOptions.Compiled);

    private static Regex PhoneReg() => new Regex(@"[^\d+]", RegexOptions.Compiled);

    private static Regex PhoneSevenReg() => new Regex(@"(\d{3})(\d{4})", RegexOptions.Compiled);

    private static Regex PhoneTenReg() => new Regex(@"(\d{3})(\d{3})(\d{4})", RegexOptions.Compiled);

#endif
}