namespace CoreOne.Http.Attributes;

/// <summary>
/// Sends an HTTP DELETE request to the specified path.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class DeleteAttribute([StringSyntax("Route")] string? path = null) : HttpMethodAttribute("DELETE", path);
