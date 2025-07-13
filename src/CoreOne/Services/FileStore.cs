namespace CoreOne.Services;

public class FileStore<T> : Disposable where T : class
{
    public string Path { get; protected set; }
    protected ISerializer Serializer { get; set; }

    public FileStore(string? path = null, bool encrypt = false)
    {
        Path = path ?? string.Empty;
        Serializer = NJsonService.Instance;
    }

    public FileStore(ISerializer serializer, string? path = null, bool encrypt = false)
    {
        Path = path ?? string.Empty;
        Serializer = serializer;
    }

    public Task<IResult<T>> Load(CancellationToken cancellationToken = default) => Load(Path, cancellationToken);

    public async Task<IResult<T>> Load(string path, CancellationToken cancellationToken = default)
    {
        var result = Result.Fail<T>("File does not exist");
        if (File.Exists(path))
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var next = await Serializer.DeserializeAsync(stream, typeof(T), cancellationToken);
                return next.Select(p => (T?)p);
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
                await fs.WriteAsync(buffer, cancellationToken);
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