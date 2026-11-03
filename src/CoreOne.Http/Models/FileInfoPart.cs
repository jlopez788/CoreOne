namespace CoreOne.Http.Models;

/// <summary>
/// Represents a multipart/form-data part backed by a <see cref="FileInfo"/>.
/// </summary>
public class FileInfoPart(FileInfo value, string? fileName = null, string? contentType = null, string? name = null)
    : MultipartItem(fileName ?? value?.Name ?? "file", contentType, name)
{
    public FileInfo Value { get; } = value ?? throw new ArgumentNullException(nameof(value));

    protected override HttpContent CreateContent() => new StreamContent(Value.OpenRead());
}
