namespace CoreOne.Http.Attributes;

/// <summary>
/// Marks an interface for REST client generation and optionally configures a base route path, named HttpClient, or default serializer key.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
public class RestClientAttribute([StringSyntax("Route")] string? basePath = null, string? clientName = null, string? serializerKey = null) : Attribute
{
    public string? BasePath { get; } = basePath;
    public string? ClientName { get; } = clientName;
    public string? SerializerKey { get; } = serializerKey;
}