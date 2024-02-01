using Microsoft.Extensions.Logging;

namespace OneCore.Extensions;

public static class LoggerExtensions
{
    public static void LogEntry(this ILogger? logger, string message, params object?[] args) => LogEntry(logger, null, message, args);

    public static void LogEntry(this ILogger? logger, Exception? ex, string message, params object?[] args)
    {
        if (logger is null)
            return;

        if (ex is not null)
        {
            if (ex is TaskCanceledException or OperationCanceledException)
                logger.LogWarning(message, args);
            else if (ex is ObjectDisposedException)
                logger.LogWarning(prefix("Object disposed..."), args);
            else if (ex is NullReferenceException)
                logger.LogWarning(prefix("Null reference exception..."), args);
            else
                logger.LogError(ex.InnerException ?? ex, message, args);
        }
        else
            logger.LogError(message, args);

        string prefix(string msg) => $"{msg} {message}";
    }
}