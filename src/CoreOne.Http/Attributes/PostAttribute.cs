namespace CoreOne.Http.Attributes;

/// <summary>
/// Sends an HTTP POST request to the specified path.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class PostAttribute([StringSyntax("Route")] string? path = null) : HttpMethodAttribute("POST", path);