namespace OneCore.Collections;

public class DataHashSet<K, V> : DataCollection<K, V, HashSet<V>> where K : notnull
{
    protected IEqualityComparer<V> ValueComparer { get; } = ReferenceEqualityComparer<V>.Default;

    public DataHashSet() : base(50) { }

    public DataHashSet(int capacity) : base(capacity) { }

    public DataHashSet(int size, IEqualityComparer<K> comparer) : base(size, comparer) => Capacity = size;

    public DataHashSet(IEqualityComparer<K> comparer, IEqualityComparer<V>? valueComparer = null) : base(comparer)
    {
        Capacity = 50;
        ValueComparer = valueComparer?? ReferenceEqualityComparer<V>.Default;
    }

    protected override HashSet<V> CreateCollection() => new(Capacity, ValueComparer);
}