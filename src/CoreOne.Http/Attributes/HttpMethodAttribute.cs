namespace CoreOne.Http.Attributes;

/// <summary>
/// Base attribute for declaring an HTTP method and relative route template on a REST interface method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public abstract class HttpMethodAttribute(string method, [StringSyntax("Route")] string? path = null) : Attribute
{
    public string Method { get; } = method;
    public string Path { get; } = path ?? string.Empty;
}