namespace CoreOne.Http.Attributes;

/// <summary>
/// Binds a method parameter to a request HTTP header with the specified name.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class HeaderAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
