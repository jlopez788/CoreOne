using CoreOne.Reactive;
using System.Diagnostics;

namespace CoreOne.Services;

/// <summary>
/// Debounced busy flag. <see cref="IsVisible"/> only turns on once work has been active for <see cref="Delay"/>,
/// and once on it stays on for at least <see cref="MinVisible"/> to avoid flicker.
/// </summary>
public class DelayedBusyState : IDisposable
{
    private readonly SafeLock Lock = new();
    private readonly Stopwatch VisibleWatch = new();
    private bool IsDisposed;
    private CancellationTokenSource? Pending;
    public TimeSpan Delay { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsVisible { get; private set; }
    public TimeSpan MinVisible { get; private set; }
    protected Subject<bool>? Stream { get; private set; } = new();

    public DelayedBusyState(TimeSpan delay = default, TimeSpan minVisible = default)
    {
        Configure(delay, minVisible);
    }

    public void Configure(TimeSpan delay, TimeSpan minVisible)
    {
        using (Lock.EnterScope())
        {
            Delay = delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
            MinVisible = minVisible < TimeSpan.Zero ? TimeSpan.Zero : minVisible;
        }
    }

    public void Dispose()
    {
        using (Lock.EnterScope())
        {
            IsDisposed = true;
            CancelPending();
            Stream?.Dispose();
            Stream = null;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>Updates the raw active state.</summary>
    /// <returns><c>true</c> when <see cref="IsVisible"/> changed synchronously (and listeners were notified).</returns>
    public bool SetActive(bool active)
    {
        bool? changed = null;
        using (Lock.EnterScope())
        {
            if (IsDisposed || IsActive == active)
                return false;

            IsActive = active;
            CancelPending();
            if (active)
            {
                // When already visible (a hide was pending) simply stay visible.
                if (!IsVisible)
                {
                    if (Delay <= TimeSpan.Zero)
                        changed = SetVisible(true);
                    else
                        Schedule(Delay);
                }
            }
            else if (IsVisible)
            {
                var remaining = MinVisible - VisibleWatch.Elapsed;
                if (remaining <= TimeSpan.Zero)
                    changed = SetVisible(false);
                else
                    Schedule(remaining);
            }
        }

        if (changed.HasValue)
            Notify(changed.Value);
        return changed.HasValue;
    }

    public void Subscribe(Action<bool> callback, CancellationToken cancellationToken) => Stream?.Subscribe(callback, cancellationToken);

    private void CancelPending()
    {
        if (Pending is null)
            return;

        Pending.Cancel();
        Pending.Dispose();
        Pending = null;
    }

    private void Notify(bool visible)
    {
        Stream?.OnNext(visible);
    }

    private void OnElapsed(CancellationTokenSource source)
    {
        bool? changed = null;
        using (Lock.EnterScope())
        {
            if (IsDisposed || !ReferenceEquals(Pending, source) || source.IsCancellationRequested)
                return;

            Pending = null;
            source.Dispose();
            if (IsVisible != IsActive)
                changed = SetVisible(IsActive);
        }

        if (changed.HasValue)
            Notify(changed.Value);
    }

    private void Schedule(TimeSpan delay)
    {
        var source = new CancellationTokenSource();
        Pending = source;
        Task.Delay(delay, source.Token)
            .ContinueWith(t => {
                if (t.Status == TaskStatus.RanToCompletion)
                    OnElapsed(source);
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private bool SetVisible(bool visible)
    {
        IsVisible = visible;
        if (visible)
            VisibleWatch.Restart();
        else
            VisibleWatch.Reset();
        return visible;
    }
}