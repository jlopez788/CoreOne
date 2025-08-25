using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace CoreOne.Services;

public class BaseService : IDisposable, IAsyncDisposable
{
    private volatile bool IsDisposed;
    [Service(Optional = true)] protected ILogger<BaseService>? Logger { get; init; } = default!;
    protected IServiceProvider? ServiceProvider { get; init; }
    protected AToken Token { get; init; } = default!;

    public BaseService(IServiceProvider? services)
    {
        Token = AToken.Create();
        ServiceProvider = services;
        ServiceInitializer.Initialize(this, services);
    }

    /* There's a bug in the framework when a class ONLY uses IAsyncDisposable.
     * An error is thrown with the following error message:
     * type only implements IAsyncDisposable. Use Dispose to dispose the container.
     */

    [Obsolete("Prefer use DisposeAsync method")]
    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;
        Task.Factory.StartNew(async () => {
            await Token.DisposeAsync();

            Dispose(true);
            await DisposeAsync(true);

            GC.SuppressFinalize(this);
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;
        await Token.DisposeAsync();

        Dispose(true);
        await DisposeAsync(true);

        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
#if NET9_0_OR_GREATER
    protected virtual ValueTask DisposeAsync(bool disposing) => ValueTask.CompletedTask;
#else
    protected virtual Task DisposeAsync(bool disposing) => Task.CompletedTask;
#endif

    protected TService Get<TService>() where TService : notnull => ServiceProvider!.GetRequiredService<TService>();

    protected void LogResult<TResult>(TResult? result, string msg, [CallerMemberName] string? name = null) where TResult : IResult
    {
        if (result is not null && Logger is not null && result.ResultType != ResultType.Success)
        {
            var type = typeof(TResult);
            var meta = MetaType.GetMetadata(type, nameof(HttpResult.StatusCode));
            var template = $"{name}::{msg}.{result.Message}";
            if (!meta.Equals(Metadata.Empty))
                template = $"{name}::StatusCode: ({meta.GetValue(result)}) {msg}.{result.Message}";
            if (result.ResultType == ResultType.Exception)
                Logger.LogEntry("Unknown Error: {0}", template);
            else if (result.ResultType == ResultType.Fail)
                Logger?.LogWarning(template);
        }
    }
}