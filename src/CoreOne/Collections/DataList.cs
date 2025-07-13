namespace CoreOne.Collections;

public class DataList<K, V> : DataCollection<K, V, List<V>> where K : notnull
{
    public DataList() : base(50) { }

    public DataList(int capacity) : base(capacity) { }

    public DataList(int size, IEqualityComparer<K> comparer) : base(size, comparer) => Capacity = size;

    public DataList(IEqualityComparer<K> comparer) : base(comparer) => Capacity = 50;

    protected override List<V> CreateCollection() => new(Capacity);

    public void RemoveAll(Func<K, V, bool> predicate)
    {
        var set = new HashSet<K>(Comparer);
        RemoveAll((k, list) => {
            list?.RemoveAll(p => predicate(k, p));
            return list is null || list.Count == 0;
        });
    }
}