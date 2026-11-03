namespace CoreOne.Http.Models;

/// <summary>
/// Represents a multipart/form-data part backed by a byte array.
/// </summary>
public class ByteArrayPart(byte[] value, string fileName, string? contentType = null, string? name = null)
    : MultipartItem(fileName, contentType, name)
{
    public byte[] Value { get; } = value ?? throw new ArgumentNullException(nameof(value));

    protected override HttpContent CreateContent() => new ByteArrayContent(Value);
}
