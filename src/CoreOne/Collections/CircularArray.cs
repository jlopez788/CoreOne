namespace CoreOne.Collections;

public sealed class CircularArray<T>(int capacity) : IEnumerable<T>
{
    private struct CircularEnumerator : IEnumerator<T>
    {
        private readonly CircularArray<T> Circular;
        private int CurrentIndex;
        private int Looped;
        public readonly T Current => Circular.Items[Pointer];
        readonly object IEnumerator.Current => Circular.Items[Pointer]!;
        private readonly int Pointer => (Circular.Head + CurrentIndex) % Circular.Capacity;

        public CircularEnumerator(CircularArray<T> circular)
        {
            Circular = circular;
            Reset();
        }

        public void Dispose()
        {
            Reset();
            GC.SuppressFinalize(this);
        }

        public bool MoveNext()
        {
            CurrentIndex--;
            return Looped++ < Circular.Count;
        }

        public void Reset()
        {
            Looped = 0;
            CurrentIndex = Circular.Count;
        }
    }

    private readonly T[] Items = new T[capacity];
    private int Head = 0;
    public int Capacity { get; } = capacity;
    public int Count { get; private set; }

    public T this[int index] => Get(index);

    public void Add(T item)
    {
        int index = (Head + Count) % Capacity; // Circular index
        Items[index] = item;

        if (Count < Capacity)
        {
            Count++;
        }
        else
        {
            Head = (Head + 1) % Capacity; // Move start forward to drop oldest element
        }
    }

    public T Get(int index)
    {
        return index < 0 || index >= Count ?
            throw new IndexOutOfRangeException("Index out of bounds") :
            Items[(Head + index) % Capacity];
    }

    IEnumerator IEnumerable.GetEnumerator() => new CircularEnumerator(this);

    public IEnumerator<T> GetEnumerator() => new CircularEnumerator(this);

    public bool Remove(T item)
    {
        for (int i = 0; i < Count; i++)
        {
            int index = (Head + i) % Capacity;
            var current = Items[index];
            if ((current is not null && ReferenceEquals(current, item)) || current?.Equals(item) == true)
            {
                for (int j = i; j < Count - 1; j++)
                {
                    int nextIndex = (Head + j + 1) % Capacity;
                    Items[(Head + j) % Capacity] = Items[nextIndex];
                }

                Items[(Head + Count - 1) % Capacity] = default; // Clear last element
                Count--;
                return true;
            }
        }
        return false;
    }
}