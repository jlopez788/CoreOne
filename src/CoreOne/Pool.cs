using System.Buffers;

namespace CoreOne;

public static class Pool
{
    public sealed class PooledBuffer<T> : IDisposable
    {
        private readonly ArrayPool<T> Pool;
        public T[] Array { get; }
        public int Size { get; }
        public T this[int index] => Array[index];

        internal PooledBuffer(ArrayPool<T> pool, T[] array, int size)
        {
            Pool = pool;
            Array = array;
            Size = size;
        }

        public static implicit operator T[](PooledBuffer<T> pool) => pool.Array;

        public void Dispose()
        {
            Pool.Return(Array);
            GC.SuppressFinalize(this);
        }
    }

    public static PooledBuffer<T> Rent<T>(int size)
    {
        var pool = ArrayPool<T>.Shared;
        var buffer = pool.Rent(size);
        return new PooledBuffer<T>(pool, buffer, size);
    }
}