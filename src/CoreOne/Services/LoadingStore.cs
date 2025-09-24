using CoreOne.Reactive;
using CoreOne.Threading.Tasks;

namespace CoreOne.Services;

public class LoadingStore : IDisposable
{
    private ImmutableList<Guid> Locks;
    public bool IsBusy { get; private set; }
    protected bool IsEmpty => Locks.IsEmpty;
    protected Subject<bool>? Stream { get; set; }

    public LoadingStore()
    {
        Locks = [];
        Stream = new Subject<bool>();
    }

    public void Dispose()
    {
        Stream?.Dispose();
        Stream = null;

        GC.SuppressFinalize(this);
    }

    public async Task<TResult?> GetResultAsync<TResult>(InvokeTask<TResult>? callback, Action<Exception>? onException = null, CancellationToken cancellationToken = default)
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
                    result = await Utility.SafeAwait(callback?.Invoke(cancellationToken));
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
                result = await Utility.SafeAwait(callback?.Invoke(cancellationToken));
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

    public Task InvokeAsync(InvokeCallback? callback, CancellationToken cancellationToken = default) => GetResultAsync(async p => {
        await Utility.SafeAwait(callback?.InvokeAsync(p));
        return 1;
    }, cancellationToken: cancellationToken);

    public void Subscribe(Action<bool> callback, SToken token) => Stream?.Subscribe(callback, token);

    private void OnCountChanged()
    {
        IsBusy = !IsEmpty;
        Stream?.OnNext(IsBusy);
    }
}