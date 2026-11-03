namespace CoreOne.Http.Attributes;

/// <summary>
/// Marks a method as sending a multipart/form-data HTTP request.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class MultipartAttribute(string boundaryText = "----CoreOneMultipartBoundary") : Attribute
{
    public string BoundaryText { get; } = boundaryText;
}
