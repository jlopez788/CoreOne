namespace CoreOne.Collections;

public sealed class ImmutableList<T> : IReadOnlyCollection<T>, IReadOnlyList<T>
{
    private struct ImmutableEnumerator(ImmutableList<T> items) : IEnumerator<T>
    {
        private readonly ImmutableList<T> Items = items;
        private int Index = -1;
        public readonly T Current => Items[Index];
        readonly object? IEnumerator.Current => Items[Index];

        public readonly void Dispose()
        { }

        public bool MoveNext()
        {
            if ((Index + 1) < Items.Count)
            {
                Index++;
                return true;
            }
            return false;
        }

        public void Reset() => Index = -1;
    }

    public static readonly ImmutableList<T> Empty = [];
    private readonly IEqualityComparer<T> Comparer;
    private readonly T[] Data;
    public int Count => Data.Length;
    public bool IsEmpty => Data == null || Data.Length == 0;

    public T this[int index] => index >= 0 && index < Count ? Data[index] : throw new ArgumentOutOfRangeException(nameof(index));

    public ImmutableList(T[]? data = null) : this(data, null) { }

    public ImmutableList(T[]? data, IEqualityComparer<T>? equalityComparer)
    {
        Data = data ?? [];
        Comparer = equalityComparer ?? EqualityComparer<T>.Default;
    }

    public ImmutableList(IEqualityComparer<T>? equalityComparer)
    {
        Data = [];
        Comparer = equalityComparer ?? EqualityComparer<T>.Default;
    }

    public ImmutableList<T> Add(T value)
    {
        var newData = new T[Data.Length + 1];

        Array.Copy(Data, newData, Data.Length);
        newData[Data.Length] = value;

        return new ImmutableList<T>(newData, Comparer);
    }

    public bool Contains(T value) => IndexOf(value) >= 0;

    public IEnumerator<T> GetEnumerator() => new ImmutableEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => new ImmutableEnumerator(this);

    public ImmutableList<T> Remove(T value)
    {
        var index = IndexOf(value);
        if (index < 0)
            return this;

        var length = Data.Length;
        return length == 1 ? Empty : RemoveAt(index);
    }

    public ImmutableList<T> RemoveAll(Predicate<T> predicate)
    {
        var items = new List<T>(Data);
        items.RemoveAll(predicate);
        return new ImmutableList<T>([.. items], Comparer);
    }

    public ImmutableList<T> RemoveAt(int index)
    {
        var length = Data.Length;
        if (index >= 0 && index < length)
        {
            if (length == 1)
                return Empty;

            var newData = new T[length - 1];
            Array.Copy(Data, 0, newData, 0, index);
            Array.Copy(Data, index + 1, newData, index, length - index - 1);
            return new ImmutableList<T>(newData, Comparer);
        }
        return this;
    }

    public T[] ToArray() => Data;

    private int IndexOf(T value)
    {
        for (var i = 0; i < Data.Length; ++i)
        {
            if (Comparer.Equals(Data[i], value))
                return i;
        }

        return -1;
    }
}