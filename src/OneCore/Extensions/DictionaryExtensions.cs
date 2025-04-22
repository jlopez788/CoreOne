namespace CoreOne.Extensions;

public static class DictionaryExtensions
{
    public static TValue? TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue>? data, TKey key, Func<TValue>? callback = null)
    {
        var value = default(TValue);
        if ((data == null || !data.TryGetValue(key, out value)) && callback != null)
        {
            value = callback.Invoke();
        }
        return value;
    }
}