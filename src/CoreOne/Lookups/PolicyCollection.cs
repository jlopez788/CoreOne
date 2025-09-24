namespace CoreOne.Lookups;

public class PolicyCollection : HashSet<Policy>
{
    public PolicyCollection()
    { }

    public PolicyCollection(Policy? policy) => Add(policy);

    public new bool Add(Policy? policy) => policy is not null && base.Add(policy);

    public void AddRange(IEnumerable<Policy>? policies)
    {
        policies?.Each(p => Add(p));
    }

    public bool Contains(PolicyCollection? policy) => Count > 0 && policy?.Count > 0 && this.Any(p => policy.Any(pi => pi.Equals(p)));

    public override string ToString()
    {
        var copy = this.Select(p => p.Description).ToArray();
        return copy.Length > 0 ? string.Join(" | ", copy) : Policy.Empty.Description ?? string.Empty;
    }
}