using System.Text.RegularExpressions;
using System.Web;

namespace OneCore;

public static partial class Utility
{
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
            return phoneNumber.Length switch {
                7 => "***-" + phoneNumber[^4..],
                10 => "(***) ***-" + phoneNumber[^4..],
                11 or 12 or 13 => code() + " (***) ***-" + phoneNumber[^4..],
                _ => "".PadRight(phoneNumber.Length - 4, '*') + phoneNumber[^4..],
            };
        }

        var (reg, format) = phoneNumber.Length switch {
            7 => (reg: PhoneSevenReg(), format: "$1-$2"),
            10 => (reg: PhoneTenReg(), format: "($1) $2-$3"),
            11 or 12 or 13 => (reg: PhonePlusReg(), format: "+$1 ($2) $3-$4"),
            _ => (reg: null, format: null)
        };
        return reg?.Replace(phoneNumber, format!) ?? phoneNumber;

        string code() => "+" + phoneNumber![..^10];
    }

    /// <summary>
    /// Safe await a nullable task
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static Task SafeAwait(Task? callback) => callback is not null ? callback : Task.CompletedTask;

    /// <summary>
    /// Safe await a nullable task
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static async Task<T?> SafeAwait<T>(Task<T>? callback) => callback is not null ? await callback : default;

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

    public static string EncodeForUrl(string value) => HttpUtility.UrlEncode(value);

    [GeneratedRegex(@"(\d{1,3})(\d{3})(\d{3})(\d{4})")]
    private static partial Regex PhonePlusReg();

    [GeneratedRegex(@"[^\d+]")]
    private static partial Regex PhoneReg();

    [GeneratedRegex(@"(\d{3})(\d{4})")]
    private static partial Regex PhoneSevenReg();

    [GeneratedRegex(@"(\d{3})(\d{3})(\d{4})")]
    private static partial Regex PhoneTenReg();
}