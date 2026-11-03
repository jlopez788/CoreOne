namespace CoreOne.Http.Attributes;

/// <summary>
/// Binds a method parameter to the URL query string. Supports primitives, collections, complex objects, and <see cref="ODataBuilder"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class QueryParamAttribute(string? alias = null, string? format = null) : Attribute
{
    public string? Alias { get; } = alias;
    public string? Format { get; } = format;
}
