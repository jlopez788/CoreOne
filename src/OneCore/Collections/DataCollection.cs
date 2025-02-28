namespace OneCore.Collections;

public abstract class DataCollection<K, V, C> : Data<K, C> where C : ICollection<V> where K : notnull
{
    protected new int Capacity { get; set; }

    public DataCollection() : this(50) { }

    public DataCollection(int capacity) : base(capacity) => Capacity = capacity;

    public DataCollection(int size, IEqualityComparer<K> comparer) : base(size, comparer) => Capacity = size;

    public DataCollection(IEqualityComparer<K> comparer) : base(comparer) => Capacity = 50;

    public void Add(KeyValuePair<K, V> item)
    {
        if (!ContainsKey(item.Key))
        {
            Add(item.Key, CreateCollection());
        }
        base[item.Key]?.Add(item.Value);
    }

    public void Add(K key, V item) => Add(new KeyValuePair<K, V>(key, item));

    public void AddRange(IEnumerable<V>? enumerable, Func<V, K> keygetter)
    {
        var groups = enumerable.ExcludeNulls().GroupBy(keygetter);
        foreach (var group in groups)
        {
            var collection = ContainsKey(group.Key) ? base[group.Key] ?? CreateCollection() : CreateCollection();
            foreach (var p in group)
                collection.Add(p);
            base[group.Key] = collection;
        }
    }

    protected abstract C CreateCollection();
}