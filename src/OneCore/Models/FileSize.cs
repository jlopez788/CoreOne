using OneCore.Converters;

namespace OneCore.Models;

[NJsonConverter(typeof(FileSizeConverter.NewtonsoftConverter))]
[SJsonConverter(typeof(FileSizeConverter.SystemJsonConverter))]
public readonly struct FileSize(long size)
{
    public string Display { get; } = FormatBytes(size);
    public long Size { get; } = size;

    public static implicit operator FileSize(long bytes) => new(bytes);

    public static implicit operator long(FileSize bytes) => bytes.Size;

    public static bool operator !=(FileSize left, FileSize right) => !(left == right);

    public static FileSize operator +(FileSize x, FileSize y) => new(x.Size + y.Size);

    public static bool operator ==(FileSize left, FileSize right) => left.Equals(right);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is FileSize size && Equals(size);

    public bool Equals(FileSize size) => Size == size.Size;

    public override int GetHashCode() => Size.GetHashCode();

    public override string ToString() => Display;

    public static string FormatBytes(long bytes)
    {
        int i;
        double dblSByte = bytes;
        string[] Suffix = ["B", "KB", "MB", "GB", "TB"];
        for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
        {
            dblSByte = bytes / 1024.0;
        }

        return $"{dblSByte:0.##} {Suffix[i]}";
    }
}
