namespace OneCore.Collections;

/// <summary>
/// Dictionary helper class
/// </summary>
/// <typeparam name="K">Type to use for Key</typeparam>
/// <typeparam name="V">Value</typeparam>
public class Data<K, V> : Dictionary<K, V> where K : notnull
{
    private K? BakDefaultKey;

    /// <summary>
    /// Default Key
    /// </summary>
    public K? DefaultKey {
        get => BakDefaultKey;
        set {
            BakDefaultKey = value;
            IsDefaultKeySet = true;
        }
    }

    /// <summary>
    /// Boolean flag
    /// </summary>
    protected bool IsDefaultKeySet { get; private set; }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">Key</param>
    /// <returns>
    /// The value associated with the specified key.
    /// If the key does not exist, it uses DefaultKey if that doesn't
    /// exist either, it returns default(V) </returns>
    public new V? this[K key] {
        get => Get(key);
        set {
            if (value is not null)
                Set(key, value);
        }
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    public Data()
    { }

    /// <summary>
    /// Initializes a new instance of the
    /// class that is empty, has the specified initial capacity, and uses the default
    /// equality comparer for the key type.
    /// </summary>
    /// <param name="size">Initial number of element it can contain</param>
    public Data(int size) : base(size) { }

    /// <summary>
    /// Initializes a new instance of the
    /// class that is empty, has the specified initial capacity, and uses the specified
    /// System.Collections.Generic.IEqualityComparer<T>.
    /// </summary>
    /// <param name="size">Initial number of element it can contain</param>
    /// <param name="comparer">
    /// The System.Collections.Generic.IEqualityComparer T implementation to use
    /// when comparing keys, or null to use the default System.Collections.Generic.EqualityComparer<T>
    /// for the type of the key.
    /// </param>
    public Data(int size, IEqualityComparer<K> comparer) : base(size, comparer) { }

    /// <summary>
    /// Initializes a new instance of the
    /// class that is empty, has the default initial capacity, and uses the specified
    /// System.Collections.Generic.IEqualityComparer T
    /// </summary>
    /// <param name="comparer">
    /// The System.Collections.Generic.IEqualityComparer T implementation to use
    /// when comparing keys, or null to use the default System.Collections.Generic.EqualityComparer<T>
    /// for the type of the key.
    /// </param>
    public Data(IEqualityComparer<K> comparer) : base(comparer) { }

    /// <summary>
    ///
    /// </summary>
    /// <param name="data"></param>
    /// <param name="comparer"></param>
    public Data(IDictionary<K, V> data, IEqualityComparer<K> comparer) : base(data, comparer) { }

    /// <summary>
    ///
    /// </summary>
    /// <param name="data"></param>
    public Data(IDictionary<K, V> data) : base(data) { }

    /// <summary>
    /// Gets item from dictionary based on the key, getter OR <see cref="DefaultKey"/>
    /// </summary>
    /// <param name="key">Primary key to use</param>
    /// <param name="getter">Use this value if key nor <see cref="DefaultKey"/> is not found</param>
    /// <returns>Value</returns>
    [return: NotNullIfNotNull(nameof(getter))]
    public virtual V? Get(K? key, Func<V>? getter = null)
    {
        V? v = default;
        var found = false;
        if (key is not null)
        {
            found = TryGetValue(key, out v);
            if (!found)
            {
                if (IsDefaultKeySet && DefaultKey is not null)
                {
                    found = TryGetValue(DefaultKey, out v);
                }
            }
        }
        if (!found && getter is not null)
        {
            v = getter();
        }
        return v;
    }

    /// <summary>
    /// Remove all items based on predicate
    /// </summary>
    /// <param name="predicate"></param>
    public void RemoveAll(Func<K, V?, bool> predicate)
    {
        var keys = this.Where((kv) => predicate(kv.Key, kv.Value))
            .SelectList(kv => kv.Key);
        foreach (var key in keys)
        {
            Remove(key);
        }
    }

    /// <summary>
    /// Adds item to dictionary if not already present
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void SafeAdd(K? key, V value)
    {
        if (key is not null && !ContainsKey(key))
            Add(key, value);
    }

    /// <summary>
    /// Adds items to the list if not present in the dictionary
    /// </summary>
    /// <param name="items"></param>
    /// <param name="keyselector"></param>
    public void SafeAddRange(IEnumerable<V>? items, Func<V, K> keyselector)
    {
        if (items is not null)
        {
            foreach (var p in items)
            {
                if (p is not null)
                {
                    var key = keyselector(p);
                    if (!ContainsKey(key))
                        Add(key, p);
                }
            }
        }
    }

    /// <summary>
    /// Sets entry in dictionary, if entry exists it overwrites
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual Data<K, V> Set(K? key, V? value) => Set(new KeyValuePair<K?, V?>(key, value));

    /// <summary>
    /// Sets entry in dictionary, if entry exists it overwrites
    /// </summary>
    /// <param name="pair"></param>
    /// <returns></returns>
    public virtual Data<K, V> Set(KeyValuePair<K?, V?> pair)
    {
        if (pair.Key is null || pair.Value is null)
            return this;

        if (!ContainsKey(pair.Key))
            Add(pair.Key, pair.Value);
        else
            base[pair.Key] = pair.Value;

        return this;
    }

    /// <summary>
    /// Sets default key to use
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Data<K, V> SetDefaultKey(K key)
    {
        DefaultKey = key;
        return this;
    }

    /// <summary>
    /// Sets items in dictionary
    /// </summary>
    /// <param name="enumerable"></param>
    /// <param name="keyselector"></param>
    /// <returns></returns>
    public virtual Data<K, V> SetRange(IEnumerable<V>? enumerable, Func<V, K> keyselector)
    {
        if (enumerable is not null)
        {
            foreach (V v in enumerable)
            {
                if (v is not null)
                    Set(keyselector(v), v);
            }
        }
        return this;
    }
}