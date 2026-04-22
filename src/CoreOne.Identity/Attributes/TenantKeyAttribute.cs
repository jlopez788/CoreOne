namespace CoreOne.Identity.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TenantKeyAttribute : Attribute
{
}