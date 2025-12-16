using CoreOne.Extensions;
using System.Security.Claims;

namespace CoreOne.Identity.Models;

public class UserIdentity : ClaimsIdentity, IEquatable<UserIdentity>
{
    private static readonly string[] EmailFields = [
        "email",
        "email_address",
        ClaimTypes.Email,
    ];
    private static readonly string[] UsernameFields = [
        "username",
        "sub",
         ClaimTypes.Upn,
    ];
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
        Permissions = FindFirst("permissions")?.Value
            .SplitBy([',', '|'])
            .Select(p => Utility.TryChangeType(p, out int num) ? num : (int?)null)
            .ExcludeNulls()
            .ToArray();

        EmailAddress = FindOne(EmailFields);
        Username = FindOne(UsernameFields);

        var expires = FindFirst("exp")?.Value;
        if (!string.IsNullOrEmpty(expires) && long.TryParse(expires, out long expSeconds))
        {
            ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
        }

        string? FindOne(string[] type) =>
            type.Select(t => FindFirst(t)?.Value)
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    }

    private UserIdentity(Guid refKey) : base([]) => RefKey = refKey.ToShortId();

    public bool Equals(UserIdentity? other) => other is not null && RefKey == other.RefKey;

    public override bool Equals(object? obj) => Equals(obj as UserIdentity);

    public override int GetHashCode() => RefKey.GetHashCode();

    public override string ToString() => IsAuthenticated ? Username ?? "" : "<Unauthorized>";
}