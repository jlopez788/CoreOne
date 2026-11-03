namespace CoreOne.Http.Attributes;

/// <summary>
/// Sends an HTTP HEAD request to the specified path.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class HeadAttribute([StringSyntax("Route")] string? path = null) : HttpMethodAttribute("HEAD", path);
