namespace CoreOne.Http.Attributes;

/// <summary>
/// Sends an HTTP PUT request to the specified path.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class PutAttribute([StringSyntax("Route")] string? path = null) : HttpMethodAttribute("PUT", path);
