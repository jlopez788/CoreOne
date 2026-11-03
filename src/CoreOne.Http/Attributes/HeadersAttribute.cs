namespace CoreOne.Http.Attributes;

/// <summary>
/// Adds static HTTP headers (in "Key: Value" format) to all requests on an interface or to a specific method.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
public class HeadersAttribute(params string[] headers) : Attribute
{
    public string[] Headers { get; } = headers ?? [];
}
