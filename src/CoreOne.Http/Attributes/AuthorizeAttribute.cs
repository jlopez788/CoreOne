namespace CoreOne.Http.Attributes;

/// <summary>
/// Binds a parameter as an Authorization header token using the specified authentication scheme (default: "Bearer").
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
public class AuthorizeAttribute(string scheme = "Bearer") : Attribute
{
    public string Scheme { get; } = scheme;
}
