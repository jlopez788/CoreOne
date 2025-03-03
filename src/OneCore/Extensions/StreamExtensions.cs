namespace OneCore.Extensions;

public static class StreamExtensions
{
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