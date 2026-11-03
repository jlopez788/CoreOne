namespace CoreOne.Http.Attributes;

/// <summary>
/// Sends an HTTP GET request to the specified path.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class GetAttribute([StringSyntax("Route")] string? path = null) : HttpMethodAttribute("GET", path);
