using System.Security.Cryptography;
using System.Text;

namespace CoreOne;

public static partial class Utility
{
    private static readonly byte[] KEY = Encoding.ASCII.GetBytes("UiU9jm6qbDmHq7vJ");

    public static IResult<string> Crc32(string? data) => Hash(new Crc32(), data);

    public static IResult<string> Crc32(byte[]? data) => Hash(new Crc32(), data);

    public static int Crc32AsInt(string? data) => CoreOne.Crc32.Compute(data);

    public static IResult<byte[]> FromBase64(string? content) => Try(() => string.IsNullOrEmpty(content) ? [] : Convert.FromBase64String(content));

    public static IResult<string> HashMD5(string? data, byte[]? key = null) => Hash(new HMACMD5(key ?? KEY), data);

    public static IResult<string> HashMD5(byte[]? data, byte[]? key = null) => Hash(new HMACMD5(key ?? KEY), data);

    public static IResult<string> HashSHA1(string? data, byte[]? key = null) => Hash(new HMACSHA1(key ?? KEY), data);

    public static IResult<string> HashSHA1(byte[]? data, byte[]? key = null) => Hash(new HMACSHA1(key ?? KEY), data);

    public static IResult<string> HashSHA256(string? data, byte[]? key = null) => Hash(new HMACSHA256(key ?? KEY), data);

    public static IResult<string> HashSHA256(byte[]? data, byte[]? key = null) => Hash(new HMACSHA256(key ?? KEY), data);

    public static IResult<string> ToBase64(byte[]? data) => Try(() => data is null ? null : Convert.ToBase64String(data));

    public static IResult<string> ToBase64(string data, Encoding? encoding = null)
    {
        encoding ??= Encoding.ASCII;
        return Try(() => Convert.ToBase64String(encoding.GetBytes(data ?? "")));
    }

    private static IResult<string> Hash(HashAlgorithm crypt, string? data) => Hash(crypt, data is null ? null : Encoding.UTF8.GetBytes(data));

    private static IResult<string> Hash(HashAlgorithm crypt, byte[]? data)
    {
        var result = Try(() => data is null ? null : crypt.ComputeHash(data))
               .Select(buffer => {
                   var str = new StringBuilder();
                   if (buffer is not null)
                       Array.ForEach(buffer, b => str.Append(b.ToString("x2")));
                   return str.ToString();
               });
        crypt.Dispose();
        return result;
    }
}