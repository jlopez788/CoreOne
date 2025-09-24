using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.Collections.Concurrent;

public class ConcurrentSet<T> : ICollection<T>
{
    private readonly HashSet<T> Set;
    private readonly SafeLock Sync = new();
    public int Count => Set.Count;

    public bool IsReadOnly => false;

    public ConcurrentSet()
    {
        Set = [];
    }

    public ConcurrentSet(IEqualityComparer<T> comparer)
    {
        Set = new HashSet<T>(comparer);
    }

    public ConcurrentSet(IEnumerable<T> items, IEqualityComparer<T> comparer)
    {
        Set = new HashSet<T>(items, comparer);
    }

    void ICollection<T>.Add(T item) => Execute(() => Set.Add(item));

    public bool Add(T item) => Execute(() => Set.Add(item));

    public void Clear() => Execute(Set.Clear);

    public bool Contains(T item) => Execute(() => Set.Contains(item));

    public void CopyTo(T[] array, int arrayIndex) => Execute(() => Set.CopyTo(array, arrayIndex));

    public void Each(Action<T> callback)
    {
        var items = ToList();
        items.Each(callback);
    }

    public Task EachAsync(Func<T, Task> callback)
    {
        var items = ToList();
        return items.EachAsync(callback);
    }

    public IReadOnlyCollection<T> Filter(Predicate<T> filter) => Execute(() => Set.Where(p => filter(p)).ToList()) ?? [];

    public IEnumerator<T> GetEnumerator() => Execute(() => Set.GetEnumerator());

    IEnumerator IEnumerable.GetEnumerator() => Execute(() => Set.GetEnumerator());

    public bool Remove(T item) => Execute(() => Set.Remove(item));

    public IReadOnlyCollection<T> ToList() => Execute(() => Set.ToList()) ?? [];

    private void Execute(Action callback)
    {
        using (Sync.EnterScope())
            Utility.Try(callback);
    }

    private R? Execute<R>(Func<R> callback)
    {
        using (Sync.EnterScope())
            return Utility.Try(callback).Model;
    }
}