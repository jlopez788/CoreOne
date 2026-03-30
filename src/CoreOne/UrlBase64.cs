using System.Buffers.Text;
using System.Text;

namespace CoreOne;

public class UrlBase64
{
    private static readonly IResult<string> EmptyResult = new Result<string>(string.Empty);

    public static IResult<string> FromUrlBase64(string? data, Encoding? encoding = null)
    {
        return data.IsNullOrEmpty() ? EmptyResult :
            Utility.Try(() => {
                var buffer = FromBase64String(data);
                return (encoding ?? Encoding.UTF8).GetString(buffer);
            });
    }

    public static IResult<string> ToUrlBase64String<T>(T? model, ISerializer? serializer = null)
    {
        return model is null ? EmptyResult :
            Utility.Try(() => {
                serializer ??= NJsonService.Instance;
                var buffer = serializer.Serialize(model);
                return ToBase64(buffer);
            });
    }

    public static IResult<string> ToUrlBase64String(string? data, Encoding? encoding = null)
    {
        return data.IsNullOrEmpty() ?
            new Result<string>(ResultType.Fail, "Empty value") :
            Utility.Try(() => {
                var buffer = (encoding ?? Encoding.UTF8).GetBytes(data);
                return ToBase64(buffer);
            });
    }

    private static byte[] FromBase64String(string data)
    {
#if NET9_0_OR_GREATER
        return Base64Url.DecodeFromChars(data);
#else
        string base64 = data
                    .Replace('-', '+')
                    .Replace('_', '/');

        base64 += (base64.Length % 4) switch {
            2 => base64 + "==",
            3 => base64 + "=",
            _ => string.Empty
        };

        return Convert.FromBase64String(base64);
#endif
    }

    private static string ToBase64(byte[] buffer)
    {
#if NET9_0_OR_GREATER
        return Base64Url.EncodeToString(buffer);
#else
        string base64 = Convert.ToBase64String(buffer);
        return base64
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
#endif
    }
}