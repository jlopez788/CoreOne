namespace CoreOne.Http.Attributes;

/// <summary>
/// Overrides the parameter or property name when binding to route templates, query strings, form fields, or multipart sections.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class AliasAsAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
