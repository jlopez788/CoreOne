namespace CoreOne.Identity.Contracts;

public interface ITenantProvider
{
    ValueTask<object?> GetTenantKey();
}