using CoreOne.Extensions;
using System.Security.Claims;

namespace CoreOne.Identity.Models;

public class UserIdentity : ClaimsIdentity, IEquatable<UserIdentity>
{
    public static readonly UserIdentity Empty = new(Guid.Empty);
    public static readonly ClaimsPrincipal Unauthorized = new(Empty);
    private readonly string RefKey;
    public string? EmailAddress { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public bool IsImpersonating { get; init; }
    public int[]? Permissions { get; init; }
    public string? Username { get; init; }

    public UserIdentity(IEnumerable<Claim> claims) : base(claims, "Cookies", "sub", ClaimTypes.Role)
    {
        RefKey = ID.Create().ToShortId();
    }

    private UserIdentity(Guid refKey) : base([])
    {
        RefKey = refKey.ToShortId();
    }

    public bool Equals(UserIdentity? other) => other is not null && RefKey == other.RefKey;

    public override bool Equals(object? obj) => Equals(obj as UserIdentity);

    public override int GetHashCode() => RefKey.GetHashCode();

    public override string ToString() => IsAuthenticated ? Username ?? "" : "<Unauthorized>";
}