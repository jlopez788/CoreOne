namespace CoreOne.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class OInjectAttribute : Attribute
{
    public Type? DefaultServiceType { get; init; }
    public bool Optional { get; init; }
}