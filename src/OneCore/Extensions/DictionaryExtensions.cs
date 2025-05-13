namespace CoreOne.Extensions;

public static class DictionaryExtensions
{
    [return: NotNullIfNotNull(nameof(getDefaultValue))]
    public static TValue? GetValue<TKey, TValue>(this IDictionary<TKey, TValue>? data, TKey? key, Func<TValue>? getDefaultValue = null) where TValue : notnull
    {
        if (key is null && getDefaultValue is not null)
            return getDefaultValue.Invoke();
        else if (key is null && getDefaultValue is null)
            return default;
        return key is not null && data?.TryGetValue(key, out var current) == true ?
            current : getDefaultValue is not null ? getDefaultValue.Invoke() : default;
    }
}