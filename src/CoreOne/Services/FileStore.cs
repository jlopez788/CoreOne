using CoreOne.Cryptography;

namespace CoreOne.Services;

public class FileStore<T>(ICypher? cypher, ISerializer? serializer, string? path = null) : Disposable where T : class
{
    public string Path { get; protected set; } = path ?? string.Empty;
    protected ICypher? Cypher { get; } = cypher;
    protected ISerializer Serializer { get; } = serializer ?? NJsonService.Instance;

    public FileStore(string? path = null) : this(null, null, path) { }

    public FileStore(ISerializer serializer, string? path = null) : this(null, serializer, path) { }

    public FileStore(ICypher cypher, string? path = null) : this(cypher, null, path) { }

    public IResult<T> Load(string? path = null)
    {
        path ??= Path;
        try
        {
            if (File.Exists(path))
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                return stream.ReadFully()
                    .SelectResult(p => Cypher?.Decrypt(p) ?? new Result<byte[]>(p))
                    .SelectResult(p => Serializer.Deserialize(p, typeof(T)))
                    .Select(p => (T)p);
            }
            else
            {
                return Result.Fail<T>("File does not exist");
            }
        }
        catch (Exception ex)
        {
            return Result.FromException<T>(ex);
        }
    }

    public Task<IResult<T>> LoadAsync(CancellationToken cancellationToken = default) => Loadsync(Path, cancellationToken);

    public async Task<IResult<T>> Loadsync(string path, CancellationToken cancellationToken = default)
    {
        var result = Result.Fail<T>("File does not exist");
        if (File.Exists(path))
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var next = await stream.ReadFullyAsync(cancellationToken);
                return next.SelectResult(p => Cypher?.Decrypt(p) ?? new Result<byte[]>(p))
                           .SelectResult(p => Serializer.Deserialize(p, typeof(T)))
                           .Select(p => (T)p);
            }
            catch (Exception ex)
            {
                result = Result.FromException<T>(ex);
            }
        }
        return result;
    }

    public Task<IResult> Save(T model, CancellationToken cancellationToken = default) => Save(model, Path, cancellationToken);

    public async Task<IResult> Save(T model, string path, CancellationToken cancellationToken = default)
    {
        IResult result;
        try
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                var buffer = Serializer.Serialize(model, typeof(T));
                buffer = Cypher?.Encrypt(buffer, null) ?? buffer;
                await fs.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
                await fs.FlushAsync(cancellationToken);
            }
            result = Result.Ok;
        }
        catch (Exception ex)
        {
            result = new Result(ResultType.Exception, ex.Message);
        }
        return result;
    }
}