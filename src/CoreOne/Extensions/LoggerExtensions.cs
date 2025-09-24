using Microsoft.Extensions.Logging;

namespace CoreOne.Extensions;

public static class LoggerExtensions
{
    public static void LogEntryX(this ILogger? logger, string message, params object?[] args) => LogEntryX(logger, null, message, args);

    public static void LogEntryX(this ILogger? logger, Exception? ex, string message, params object?[] args)
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
