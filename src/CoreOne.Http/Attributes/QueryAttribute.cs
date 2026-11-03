namespace CoreOne.Http.Attributes;

/// <summary>
/// Sends an HTTP QUERY request to the specified path.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class QueryAttribute([StringSyntax("Route")] string? path = null) : HttpMethodAttribute("QUERY", path);