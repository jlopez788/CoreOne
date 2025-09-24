namespace CoreOne.Extensions;

public static class DictionaryExtensions
{
    public static V? GetSet<K, V>(this IDictionary<K, V>? data, K key, Func<V> getter)
    {
        V? k = default;
        if (data != null)
        {
            if (!data.TryGetValue(key, out var value))
            {
                k = getter();
                data.Add(key, k);
            }
            else
                k = value;
        }
        return k;
    }

    [return: NotNullIfNotNull(nameof(getDefaultValue))]
    public static TValue? GetValue<TKey, TValue>(this IDictionary<TKey, TValue>? data, TKey? key, Func<TValue>? getDefaultValue = null) where TValue : notnull
    {
        if (key is null && getDefaultValue is not null)
            return getDefaultValue.Invoke();
        else if (key is null && getDefaultValue is null)
            return default;
        return key is not null && data?.TryGetValue(key, out var current) == true && current is not null ?
            current : getDefaultValue is not null ? getDefaultValue.Invoke() : default;
    }
}