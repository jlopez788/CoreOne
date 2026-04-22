namespace CoreOne.Identity.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class TenantOwnedAttribute : Attribute
{
}