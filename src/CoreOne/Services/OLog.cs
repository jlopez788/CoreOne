using Microsoft.Extensions.Logging;

namespace CoreOne.Services;

[Service]
public class OLog<T>(ILogger<T> logger)
{
    public void LogError(string message)
    {
        if (IsEnabled(LogLevel.Error))
            logger.LogError(message);
    }

    public void LogError<T0>(string message, T0 arg0)
    {
        if (IsEnabled(LogLevel.Error))
            logger.LogError(message, arg0);
    }

    public void LogError<T0, T1>(string message, T0 arg0, T1 arg1)
    {
        if (IsEnabled(LogLevel.Error))
            logger.LogError(message, arg0, arg1);
    }

    public void LogError<T0, T1, T2>(string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (IsEnabled(LogLevel.Error))
            logger.LogError(message, arg0, arg1, arg2);
    }

    public void LogError(Exception exception, string message)
    {
        if (exception is TaskCanceledException or OperationCanceledException)
        {
            LogWarning(message);
            return;
        }
        if (IsEnabled(LogLevel.Error))
            logger.LogError(exception, message);
    }

    public void LogError<T0>(Exception exception, string message, T0 arg0)
    {
        if (exception is TaskCanceledException or OperationCanceledException)
        {
            LogWarning(message, arg0);
            return;
        }
        if (IsEnabled(LogLevel.Error))
            logger.LogError(exception, message, arg0);
    }

    public void LogError<T0, T1>(Exception exception, string message, T0 arg0, T1 arg1)
    {
        if (exception is TaskCanceledException or OperationCanceledException)
        {
            LogWarning(message, arg0, arg1);
            return;
        }
        if (IsEnabled(LogLevel.Error))
            logger.LogError(exception, message, arg0, arg1);
    }

    public void LogError<T0, T1, T2>(Exception exception, string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (exception is TaskCanceledException or OperationCanceledException)
        {
            LogWarning(message, arg0, arg1, arg2);
            return;
        }
        if (IsEnabled(LogLevel.Error))
            logger.LogError(exception, message, arg0, arg1, arg2);
    }

    public void LogInformation(string message)
    {
        if (IsEnabled(LogLevel.Information))
            logger.LogInformation(message);
    }

    public void LogInformation<T0>(string message, T0 arg0)
    {
        if (IsEnabled(LogLevel.Information))
            logger.LogInformation(message, arg0);
    }

    public void LogInformation<T0, T1>(string message, T0 arg0, T1 arg1)
    {
        if (IsEnabled(LogLevel.Information))
            logger.LogInformation(message, arg0, arg1);
    }

    public void LogInformation<T0, T1, T2>(string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (IsEnabled(LogLevel.Information))
            logger.LogInformation(message, arg0, arg1, arg2);
    }

    public void LogWarning(string message)
    {
        if (IsEnabled(LogLevel.Warning))
            logger.LogWarning(message);
    }

    public void LogWarning<T0>(string message, T0 arg0)
    {
        if (IsEnabled(LogLevel.Warning))
            logger.LogWarning(message, arg0);
    }

    public void LogWarning<T0, T1>(string message, T0 arg0, T1 arg1)
    {
        if (IsEnabled(LogLevel.Warning))
            logger.LogWarning(message, arg0, arg1);
    }

    public void LogWarning<T0, T1, T2>(string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (IsEnabled(LogLevel.Warning))
            logger.LogWarning(message, arg0, arg1, arg2);
    }

    private bool IsEnabled(LogLevel level) => logger.IsEnabled(level);
}