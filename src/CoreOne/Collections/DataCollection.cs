namespace CoreOne.Collections;

public abstract class DataCollection<K, V, C> : Data<K, C> where C : ICollection<V> where K : notnull
{
   

    public DataCollection() : this(50) { }

    public DataCollection(int capacity) : base(capacity) { }

    public DataCollection(int size, IEqualityComparer<K> comparer) : base(size, comparer) { }

    public DataCollection(IEqualityComparer<K> comparer) : base(comparer) { }

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