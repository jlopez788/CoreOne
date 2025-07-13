namespace CoreOne.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class ExcludeFromValidationAttribute : Attribute
{
}