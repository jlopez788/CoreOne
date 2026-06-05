using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CoreOne.Services;

internal class RunOnceHostedService(IServiceProvider sp, Func<IServiceProvider, CancellationToken, Task> callback) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<OLog<RunOnceHostedService>>();
        logger.LogInformation("Starting RunOnceHostedService");
        try
        {
            await callback.Invoke(scope.ServiceProvider, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in RunOnceHostedService");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}