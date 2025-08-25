namespace CoreOne.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// Returns an updated list with the model added / updated on the list.
    /// If the list is null, it creates a new list and appends given model
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection">Collection to add the model to</param>
    /// <param name="model">Model to add/update on the collection</param>
    /// <param name="comparer">Compare given model for updating</param>
    /// <returns></returns>
    public static List<T> AddOrUpdate<T>(this List<T>? collection, T? model, Predicate<T>? comparer = null)
    {
        var isFound = false;
        collection ??= new List<T>(5);
        if (model is null)
            return collection;

        if (comparer is not null)
        {
            for (int i = 0; i < collection.Count && !isFound; i++)
            {
                isFound = comparer.Invoke(collection[i]);
                if (isFound)
                {
                    collection[i] = model;
                }
            }
        }
        if (!isFound)
        {
            collection.Add(model);
        }

        return collection;
    }

    /// <summary>
    /// Adds a list of items to a set
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="set">Set</param>
    /// <param name="items">Items to add to the set</param>
    public static void AddRange<T>(this ISet<T>? set, IEnumerable<T>? items)
    {
        if (set is not null && items is not null)
        {
            foreach (var item in items)
                set.Add(item);
        }
    }

    /// <summary>
    /// Applies an accumulator function over a sequence
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TAccumulate"></typeparam>
    /// <param name="source"></param>
    /// <param name="seed"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    public static async Task<TAccumulate> AggregateAsync<TSource, TAccumulate>(this IEnumerable<TSource>? source, TAccumulate seed, Func<TAccumulate, TSource, Task<TAccumulate>> func)
    {
        if (source is null || !source.Any())
            return seed;

        var next = seed;
        foreach (var item in source)
            next = await func(next, item);

        return next;
    }

    /// <summary>
    /// Applies an accumulator function over a sequence, while Result is successful
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TAccumulate"></typeparam>
    /// <param name="source"></param>
    /// <param name="seed"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    public static TAccumulate AggregateResult<TSource, TAccumulate>(this IEnumerable<TSource>? source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func) where TAccumulate : IResult
    {
        if (source is null || !source.Any())
            return seed;

        var next = seed;
        foreach (var item in source)
        {
            if (next.Success)
                next = func(next, item);
            if (!next.Success)
                break;
        }
        return next;
    }

    /// <summary>
    /// Applies an accumulator function over a sequence, while Result is successful
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TAccumulate"></typeparam>
    /// <param name="source"></param>
    /// <param name="seed"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    public static async Task<TAccumulate> AggregateResultAsync<TSource, TAccumulate>(this IEnumerable<TSource>? source, TAccumulate seed, Func<TAccumulate, TSource, Task<TAccumulate>> func) where TAccumulate : IResult
    {
        if (source is null || !source.Any())
            return seed;

        var next = seed;
        foreach (var item in source)
        {
            if (next.Success)
                next = await func(next, item);
            if (!next.Success)
                break;
        }
        return next;
    }

    /// <summary>
    /// Enumerates through each item in the list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="callback"></param>
    public static void Each<T>(this IEnumerable<T>? items, Action<T> callback)
    {
        if (items is not null)
        {
            foreach (var p in items)
                callback.Invoke(p);
        }
    }

    /// <summary>
    /// Enumerates through each item in the list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="callback"></param>
    public static void Each<T>(this IEnumerable<T>? items, Action<T, int> callback)
    {
        if (items is not null && callback is not null)
        {
            items.Select((item, i) => new { item, i }).Each(p => callback(p.item, p.i));
        }
    }

    /// <summary>
    /// Synchronously iterates over every task
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="callback"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async Task EachAsync<T>(this IEnumerable<T>? items, Func<T, Task> callback, CancellationToken token = default)
    {
        if (items is not null && callback is not null)
        {
            foreach (var p in items)
            {
                if (token.IsCancellationRequested)
                    break;
                await callback(p);
            }
        }
    }

    /// <summary>
    /// Removes all null / empty strings from sequence
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public static IEnumerable<string> ExcludeNullOrEmpty(this IEnumerable<string?>? items)
    {
        if (items is not null)
        {
            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item))
                    yield return item!;
            }
        }
    }

    /// <summary>
    /// Returns a list excluding all null values within the list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">List of items</param>
    /// <returns>Removes all null values from list</returns>
    public static IEnumerable<T> ExcludeNulls<T>(this IEnumerable<T?>? items)
    {
        if (items is not null)
        {
            foreach (var item in items)
            {
                if (item is not null)
                    yield return item;
            }
        }
    }

    /// <summary>
    /// Returns a list excluding all null values within the list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">List of items</param>
    /// <returns>Removes all null values from list</returns>
    public static IEnumerable<T> ExcludeNulls<T>(this IEnumerable<T?>? items) where T : struct
    {
        if (items is not null)
        {
            foreach (var item in items)
            {
                if (item.HasValue)
                    yield return item.Value;
            }
        }
    }

    /// <summary>
    /// Split sequence into chunks
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="partitionSize"></param>
    /// <returns></returns>
    public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T>? items, int partitionSize)
    {
        var partition = new List<T>(partitionSize);
        if (items is not null)
        {
            foreach (T item in items)
            {
                partition.Add(item);
                if (partition.Count == partitionSize)
                {
                    yield return partition;
                    partition = new List<T>(partitionSize);
                }
            }

            // Cope with items.Count % partitionSize != 0
            if (partition.Count > 0)
            {
                yield return partition;
            }
        }
    }

    public static R[] SelectArray<T, R>(this IEnumerable<T>? enumerable, Func<T, R> callback) => enumerable is not null && callback is not null ? [.. enumerable.Select(callback)] : [];

    /// <summary>
    /// Maps an enumerable to list of given type <typeparamref name="R"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="R"></typeparam>
    /// <param name="enumerable"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static List<R> SelectList<T, R>(this IEnumerable<T>? enumerable, Func<T, R> callback) => enumerable is not null && callback is not null ? [.. enumerable.Select(callback)] : [];

    /// <summary>
    /// Creates data dictionary from given items
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="items"></param>
    /// <param name="keyGetter"></param>
    /// <param name="valueGetter"></param>
    /// <returns></returns>
    public static Data<TKey, TValue> ToData<T, TKey, TValue>(this IEnumerable<T>? items, Func<T, TKey> keyGetter, Func<T, TValue> valueGetter) where TKey : notnull
    {
        var data = new Data<TKey, TValue>();
        if (items is not null)
        {
            foreach (var item in items)
            {
                var key = keyGetter(item);
                var value = valueGetter(item);
                data.Set(key, value);
            }
        }
        return data;
    }

    /// <summary>
    /// Creates data dictionary from given items
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="items"></param>
    /// <param name="keyGetter"></param>
    /// <param name="valueGetter"></param>
    /// <param name="comparer"></param>
    /// <returns></returns>
    public static Data<TKey, TValue> ToData<T, TKey, TValue>(this IEnumerable<T>? items, Func<T, TKey> keyGetter, Func<T, TValue> valueGetter, IEqualityComparer<TKey> comparer) where TKey : notnull
    {
        var data = new Data<TKey, TValue>(comparer);
        if (items is not null)
        {
            foreach (var item in items)
            {
                var key = keyGetter(item);
                var value = valueGetter(item);
                data.Set(key, value);
            }
        }
        return data;
    }

    /// <summary>
    /// Creates data dictionary from given items
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="items"></param>
    /// <param name="keyGetter"></param>
    /// <returns></returns>
    public static Data<TKey, T> ToData<T, TKey>(this IEnumerable<T>? items, Func<T, TKey> keyGetter) where TKey : notnull
    {
        var data = new Data<TKey, T>();
        if (items is not null)
        {
            foreach (var item in items)
            {
                var key = keyGetter(item);
                data.Set(key, item);
            }
        }
        return data;
    }

    /// <summary>
    /// Creates data dictionary from given items
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="items"></param>
    /// <param name="keyGetter"></param>
    /// <param name="comparer"></param>
    /// <returns></returns>
    public static Data<TKey, T> ToData<T, TKey>(this IEnumerable<T>? items, Func<T, TKey> keyGetter, IEqualityComparer<TKey> comparer) where TKey : notnull
    {
        var data = new Data<TKey, T>(comparer);
        if (items is not null)
        {
            foreach (var item in items)
            {
                var key = keyGetter(item);
                data.Set(key, item);
            }
        }
        return data;
    }

    /// <summary>
    /// Converts metadata collection to a dictionary, accessible by property namess
    /// </summary>
    /// <param name="entries"></param>
    /// <returns></returns>
    public static Data<string, Metadata> ToDictionary(this IReadOnlyCollection<Metadata>? entries)
    {
        if (entries is not null)
        {
            var data = entries.ToDictionary(p => p.Name, p => p);
            return new Data<string, Metadata>(data, MStringComparer.OrdinalIgnoreCase);
        }
        return [];
    }

    /// <summary>
    /// Maps and filters an enumerable into a list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static List<T> ToList<T>(this IEnumerable<T>? items, Func<T, bool> predicate) => items?.Where(predicate).ToList() ?? [];
}