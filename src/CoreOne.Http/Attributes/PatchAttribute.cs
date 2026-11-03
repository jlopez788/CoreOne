namespace CoreOne.Http.Attributes;

/// <summary>
/// Sends an HTTP PATCH request to the specified path.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class PatchAttribute([StringSyntax("Route")] string? path = null) : HttpMethodAttribute("PATCH", path);
