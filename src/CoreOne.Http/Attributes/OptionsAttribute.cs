namespace CoreOne.Http.Attributes;

/// <summary>
/// Sends an HTTP OPTIONS request to the specified path.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class OptionsAttribute([StringSyntax("Route")] string? path = null) : HttpMethodAttribute("OPTIONS", path);
