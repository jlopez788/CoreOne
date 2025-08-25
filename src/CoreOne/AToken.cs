namespace CoreOne;

public class AToken : IAsyncDisposable
{
    public static readonly AToken Empty = new(Guid.Empty, null);
    internal CancellationTokenSource? TokenSource;
    private readonly SafeLock Sync = new();
    private ImmutableList<Func<Task>> Tasks;
    public string Id { get; }
    public bool IsCancellationRequested => TokenSource is null || (TokenSource is not null && TokenSource.IsCancellationRequested);
    protected SToken SToken { get; }

    private AToken(Guid? id, CancellationTokenSource? tokenSource)
    {
        TokenSource = tokenSource;
        Tasks = [];
        Id = id.GetValueOrDefault(ID.Create()).ToShortId();
        SToken = new SToken(this);
    }

    public static AToken Create() => new(Guid.NewGuid(), new CancellationTokenSource());

    public static implicit operator CancellationToken(AToken? subscriberToken) => subscriberToken?.TokenSource?.Token ?? CancellationToken.None;

    public static implicit operator SToken(AToken? token) => token?.SToken ?? SToken.Empty;

    public async ValueTask DisposeAsync()
    {
        var tasks = Array.Empty<Func<Task>>();
        try
        {
            if (TokenSource is not null)
            {
                using (Sync.EnterScope())
                {
                    tasks = [.. Tasks];
                    TokenSource?.Dispose();
                    TokenSource = null;
                }
            }
        }
        catch { }

        await tasks.EachAsync(p => p.Invoke());
        using (Sync.EnterScope())
            Tasks = [];

        GC.SuppressFinalize(this);
    }

    public override bool Equals(object? obj) => obj is AToken sb && Id == sb.Id;

    public override int GetHashCode() => Id.GetHashCode();

    public void Register(IDisposable disposable) => Register(() => disposable.Dispose());

    public void Register(Action callback) => Register(callback.AsTask());

    public void Register(Func<Task> callback)
    {
        using (Sync.EnterScope())
            Tasks = Tasks.Add(callback);
    }

    public void RegisterAsync(IAsyncDisposable asyncDisposable) => Register(() => Task.Factory.StartNew(asyncDisposable.DisposeAsync));

    public override string ToString() => $"Token: {Id}. Cancelled? {(IsCancellationRequested ? "YES" : "")}";
}