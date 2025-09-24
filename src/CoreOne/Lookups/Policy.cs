using System.Diagnostics;

namespace CoreOne.Lookups;

[DebuggerDisplay("{Key} - {Description}")]
public class Policy : LookupType<Policy>
{
    public static readonly Policy Empty = new(0, "", "None");

    public int Key { get; }

    protected Policy(int key, string code, string? description = null) : base(code, description) => Key = key;

    public static PolicyCollection FromPolicyIds(int[]? ids)
    {
        var policies = new PolicyCollection();
        if (ids?.Length > 0)
        {
            var all = ids.Select(n => FindType(n)).ExcludeNulls().ToArray();
            var mapped = ids.Select(n => FindType(n)).ToArray();

            policies.AddRange(all);
        }
        return policies;
    }

    public static PolicyCollection operator |(Policy policyA, Policy policyB) => [policyA, policyB];

    public static PolicyCollection operator |(PolicyCollection? collection, Policy policy)
    {
        collection ??= [];
        collection.Add(policy);
        return collection;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(obj, this) || obj switch {
            Policy p => Code.Matches(p.Code) || Key == p.Key,
            string c => Code.Matches(c),
            int n => Key == n,
            _ => false,
        };
    }

    public override int GetHashCode() => !string.IsNullOrEmpty(Code) ? Code.GetHashCode() : 0;

    public override string ToString() => Description ?? string.Empty;
}