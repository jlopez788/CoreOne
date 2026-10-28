using CoreOne.Reactive;
using CoreOne.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace CoreOne.Services;

[Service(ServiceLifetime.Transient)]
public class LoadingStore : IDisposable
{
    private readonly DelayedBusyState BusyState;
    private ImmutableList<Guid> Locks;
    /// <summary>Busy state consumers should render from; honors the configured busy delay and minimum duration.</summary>
    public bool IsBusy => BusyState.IsVisible;
    /// <summary>Raw state: <c>true</c> as soon as any work is in progress, regardless of the busy delay.</summary>
    public bool IsWorking => !IsEmpty;
    protected bool IsEmpty => Locks.IsEmpty;
    protected Subject<bool>? Stream { get; set; }

    public LoadingStore() : this(TimeSpan.Zero)
    { }

    public LoadingStore(TimeSpan busyDelay, TimeSpan minBusyDuration = default)
    {
        Locks = [];
        Stream = new Subject<bool>();
        BusyState = new DelayedBusyState(busyDelay, minBusyDuration, visible => Stream?.OnNext(visible));
    }

    /// <summary>Sets how long work must run before <see cref="IsBusy"/> turns on, and how long it then stays on at minimum.</summary>
    public void Configure(TimeSpan busyDelay, TimeSpan minBusyDuration = default) => BusyState.Configure(busyDelay, minBusyDuration);

    public void Dispose()
    {
        BusyState.Dispose();
        Stream?.Dispose();
        Stream = null;

        GC.SuppressFinalize(this);
    }

    public async ValueTask<TResult?> GetResultAsync<TResult>(InvokeTask<TResult>? callback, Action<Exception>? onException = null, CancellationToken cancellationToken = default)
    {
        TResult? result = default;
        if (callback != null)
        {
            using var token = MarkBusy();
            try
            {
#if DEBUG
                try
                {
                    result = await Utility.SafeAwait(callback?.Invoke(cancellationToken).AsTask());
                }
                catch (OperationCanceledException)
                {// do nothing..
                }
                catch (ObjectDisposedException oex)
                {// Ignore this type of exception. Gives us nothing for troubleshooting
                    Console.WriteLine(oex.Message);
                }
                catch (Exception ex)
                { // Easier to troubleshoot where we went wrong...
                    System.Diagnostics.Debugger.Break();
                    Console.WriteLine(ex.Message);
                    await callback.Invoke(cancellationToken);
                }
#else
                result = await Utility.SafeAwait(callback?.Invoke(cancellationToken).AsTask());
#endif
            }
            catch (OperationCanceledException)
            {// do nothing..
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
            }
        }
        return result;
    }

    public IDisposable MarkBusy()
    {
        var id = Guid.NewGuid();
        var token = new Subscription(() => {
            Locks = Locks.Remove(id);
            OnCountChanged();
        });
        Locks = Locks.Add(id);
        OnCountChanged();
        return token;
    }

    public async ValueTask InvokeAsync(Action callback, CancellationToken cancellationToken = default) => await GetResultAsync(_ => {
        callback.Invoke();
        return new ValueTask<int>(1);
    }, null, cancellationToken);

    public async ValueTask InvokeAsync(Func<Task> callback, CancellationToken cancellationToken) => await GetResultAsync(async _ => {
        await Utility.SafeAwait(callback.Invoke());
        return 1;
    }, null, cancellationToken);

    public async ValueTask InvokeAsync(InvokeCallback? callback, CancellationToken cancellationToken = default) => await GetResultAsync(async p => {
        await Utility.SafeAwait(callback?.InvokeAsync(p));
        return 1;
    }, null, cancellationToken);

    public void Subscribe(Action<bool> callback, SToken token) => Stream?.Subscribe(callback, token);

    private void OnCountChanged()
    {
        // Visible-state changes notify through the BusyState callback; otherwise still notify so completed work renders.
        if (!BusyState.SetActive(!IsEmpty))
            Stream?.OnNext(IsBusy);
    }
}