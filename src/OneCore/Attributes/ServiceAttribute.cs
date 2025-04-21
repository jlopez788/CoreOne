namespace CoreOne.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class ServiceAttribute : Attribute
{
    public Type? DefaultServiceType { get; init; }
    public bool Optional { get; init; }
}