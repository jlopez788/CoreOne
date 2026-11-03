namespace CoreOne.Http.Models;

/// <summary>
/// Represents a multipart/form-data part backed by a <see cref="Stream"/>.
/// </summary>
public class StreamPart(Stream value, string fileName, string? contentType = null, string? name = null)
    : MultipartItem(fileName, contentType, name)
{
    public Stream Value { get; } = value ?? throw new ArgumentNullException(nameof(value));

    protected override HttpContent CreateContent() => new StreamContent(Value);
}
