using CoreOne.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace CoreOne.Identity.Models;

public readonly struct BearerToken : IEqualityComparer<BearerToken>, IEquatable<BearerToken>
{
    public static readonly BearerToken Empty = new(null);
    private readonly string? Token;
    public bool IsEmpty { get; }
    public int Length { get; }

    public BearerToken()
    {
        IsEmpty = true;
        Token = null;
        Length = 0;
    }

    public BearerToken(string? token)
    {
        IsEmpty = true;
        Token = token;
        Length = token?.Length ?? 0;
        if (!string.IsNullOrEmpty(Token))
        {
            IsEmpty = false;
            Token = Token.Replace("Bearer", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            Length = Token.Length;
        }
        else
            Token = null;
    }

    public static implicit operator BearerToken(string? token) => new(token);

    public static implicit operator string?(BearerToken token) => token.Token;

    public static bool operator !=(BearerToken left, BearerToken right) => !left.Equals(right);

    public static bool operator ==(BearerToken left, BearerToken right) => left.Equals(right);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is BearerToken bearer && Token.Matches(bearer.Token);

    public bool Equals(BearerToken x, BearerToken y) => x.Token.Matches(y.Token);

    public bool Equals(BearerToken other) => Token.Matches(other.Token);

    public override int GetHashCode() => Token?.GetHashCode() ?? 0;

    public int GetHashCode([DisallowNull] BearerToken obj) => obj.GetHashCode();

    public override string ToString() => Token ?? string.Empty;
}