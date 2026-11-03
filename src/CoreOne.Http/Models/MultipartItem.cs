namespace CoreOne.Http.Models;

/// <summary>
/// Base class for typed multipart/form-data items such as streams, byte arrays, and files.
/// </summary>
public abstract class MultipartItem(string fileName, string? contentType = null, string? name = null)
{
    public string? ContentType { get; } = contentType;
    public string FileName { get; } = fileName;
    public string? Name { get; } = name;

    public HttpContent ToHttpContent()
    {
        var content = CreateContent();
        if (!string.IsNullOrWhiteSpace(ContentType))
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(ContentType);

        return content;
    }

    protected abstract HttpContent CreateContent();
}
