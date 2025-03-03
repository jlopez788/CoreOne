using System.Buffers;

namespace OneCore.Extensions;

public static class StreamExtensions
{
    public static async Task<IResult<byte[]>> ReadFully<T>(this T? stream) where T : Stream
    {
        try
        {
            if (stream is null)
                return new Result<byte[]>([]);

            const int LEN = 4000;
            var buffer = ArrayPool<byte>.Shared.Rent(LEN);
            using var ms = new MemoryStream();
            int read = 0;
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                await ms.WriteAsync(buffer.AsMemory(0, read));
            await ms.FlushAsync();
            ArrayPool<byte>.Shared.Return(buffer);
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
            while ((read = await stream.ReadAsync(pool.Array.AsMemory(0, LEN), cancellationToken)) > 0)
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