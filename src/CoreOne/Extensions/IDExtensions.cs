using System.Buffers.Text;
using System.Runtime.InteropServices;

namespace CoreOne.Extensions;

public static class IDExtensions
{
    private const char Dash = '-';
    private const byte ForwardSlashByte = (byte)'/';
    private const byte PlusByte = (byte)'+';
    private const char Underscore = '_';

    public static string ToShortId(this Guid guid) => Convert.ToBase64String(guid.ToByteArray()).Replace("+", "").Replace("/", "").Replace("=", "");

    public static string ToSlugUrl(this Guid guid)
    {
        Span<byte> guidBytes = stackalloc byte[16];
        Span<byte> encodedBytes = stackalloc byte[24];
#if NET6
        MemoryMarshal.TryWrite(guidBytes, ref guid); // write bytes from the Guid
#endif
#if NET8_0_OR_GREATER
        MemoryMarshal.TryWrite(guidBytes, in guid); // write bytes from the Guid
#endif
        Base64.EncodeToUtf8(guidBytes, encodedBytes, out _, out _);
        Span<char> chars = stackalloc char[22];

        // replace any characters which are not URL safe
        // skip the final two bytes as these will be '==' padding we don't need
        for (var i = 0; i < 22; i++)
        {
            chars[i] = encodedBytes[i] switch {
                ForwardSlashByte => Dash,
                PlusByte => Underscore,
                _ => (char)encodedBytes[i],
            };
        }
#if NET9_0_OR_GREATER
        return new string(chars);
#else
        return new string(chars.ToArray());
#endif

    }
}