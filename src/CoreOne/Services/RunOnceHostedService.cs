using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreOne.Services;

internal class RunOnceHostedService(IServiceProvider sp, Func<IServiceProvider, CancellationToken, Task> callback) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var logger = sp.GetRequiredService<OLog<RunOnceHostedService>>();
        logger.LogInformation("Starting RunOnceHostedService");
        try
        {
            await callback.Invoke(sp, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in RunOnceHostedService");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}