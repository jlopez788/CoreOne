using System.ComponentModel;

namespace OneCore;

/// <summary>
/// Wrapper around <see cref="CancellationTokenSource"/>
/// </summary>
public class SToken : IComponent, IDisposable
{
    public event EventHandler? Disposed;

    public static readonly SToken Empty = new(Guid.Empty, null);
    private readonly AToken? AToken;
    private readonly object Sync = new();
    private CancellationTokenSource? TokenSource;
    public string Id { get; }
    public bool IsCancellationRequested => TokenSource is null || (TokenSource is not null && TokenSource.IsCancellationRequested);
    public ISite? Site { get; set; }

    internal SToken(AToken token)
    {
        AToken = token;
        Id = token.Id;
        TokenSource = token.TokenSource;
    }

    private SToken(Guid? id, CancellationTokenSource? tokenSource)
    {
        Site = null;
        Disposed = null;
        TokenSource = tokenSource;
        Id = id.GetValueOrDefault(Guid.NewGuid()).ToShortId();
    }

    public static SToken Create() => new(Guid.NewGuid(), new CancellationTokenSource());

    public static SToken CreateLinkedTokens(params CancellationToken[] tokens)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(tokens);
        return new SToken(Guid.NewGuid(), source);
    }

    public static implicit operator CancellationToken(SToken? subscriberToken) => subscriberToken?.TokenSource?.Token ?? CancellationToken.None;

    public static bool operator !=(SToken left, SToken right) => !(left == right);

    public static bool operator ==(SToken left, SToken right) => left.Equals(right);

    public void Cancel()
    {
        try
        {
            if (TokenSource is not null)
            {
                lock (Sync)
                {
                    TokenSource?.Cancel();
                    TokenSource = null;
                }
            }
        }
        finally { }
    }

    public void Dispose()
    {
        if (AToken is not null)
        {
            _ = AToken.DisposeAsync();
        }
        try
        {
            if (TokenSource is not null)
            {
                lock (Sync)
                {
                    TokenSource?.Cancel();
                    TokenSource?.Dispose();
                    TokenSource = null;
                }
            }
        }
        finally { }

        Disposed?.Invoke(this, EventArgs.Empty);
        GC.SuppressFinalize(this);
    }

    public override bool Equals(object? obj) => obj is SToken sb && Id == sb.Id;

    public override int GetHashCode() => Id.GetHashCode();

    public void Register(IDisposable disposable) => TokenSource?.Token.Register(() => disposable.Dispose());

    public void Register(Action? callback = null)
    {
        try
        {
            if (callback is not null)
            {
                if (TokenSource is not null)
                    TokenSource.Token.Register(callback);
                else
                    callback();
            }
        }
        finally { }
    }

    public void Register(Action<object?> callback, object state)
    {
        try
        {
            if (callback is not null)
            {
                if (TokenSource is not null)
                    TokenSource.Token.Register(callback, state);
                else
                    callback(state);
            }
        }
        finally { }
    }

    public void ThrowIfCancellationRequested()
    {
        TokenSource?.Token.ThrowIfCancellationRequested();
    }

    public override string ToString() => $"Token: {Id}. Cancelled? {(IsCancellationRequested ? "YES" : "")}";

    public void Wait()
    {
        TokenSource?.Token.WaitHandle.WaitOne();
    }
}