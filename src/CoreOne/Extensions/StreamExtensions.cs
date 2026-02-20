using System.Buffers;

namespace CoreOne.Extensions;

public static class StreamExtensions
{
    public static IResult<byte[]> ReadFully<T>(this T? stream) where T : Stream
    {
        try
        {
            if (stream is null)
                return new Result<byte[]>([]);

            const int LEN = 4000;
            using var pool = Pool.Rent<byte>(LEN);
            using var ms = new MemoryStream();
            int read = 0;
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            while ((read = stream.Read(pool.Array, 0, pool.Array.Length)) > 0)
#if NET9_0_OR_GREATER
                ms.Write(pool.Array, 0, read);
#else
                 ms.Write(pool.Array, 0, read);
#endif
            ms.Flush();
            return new Result<byte[]>(ms.ToArray());
        }
        catch (Exception ex) { return Result.FromException<byte[]>(ex); }
    }

    public static async Task<IResult<byte[]>> ReadFullyAsync<TStream>(this TStream stream, CancellationToken cancellationToken = default) where TStream : Stream
    {
        try
        {
            int read;
            const int LEN = 4000;
            using var pool = Pool.Rent<byte>(LEN);
            using var buffer = new MemoryStream();
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
#if NET9_0_OR_GREATER
            while ((read = await stream.ReadAsync(pool.Array.AsMemory(0, LEN), cancellationToken)) > 0)
#else
            while ((read = await stream.ReadAsync(pool.Array, 0, LEN, cancellationToken)) > 0)
#endif
                buffer.Write(pool, 0, read);
            await buffer.FlushAsync(cancellationToken);
            return new Result<byte[]>(buffer.ToArray());
        }
        catch (Exception ex)
        {
            return Result.FromException<byte[]>(ex);
        }
    }
}