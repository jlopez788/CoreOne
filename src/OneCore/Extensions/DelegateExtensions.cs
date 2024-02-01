namespace OneCore.Extensions;

public static class DelegateExtensions
{
    /// <summary>
    /// Converts an Action to a Task
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static Func<Task> AsTask(this Action? callback) => () => {
        callback?.Invoke();
        return Task.CompletedTask;
    };

    /// <summary>
    /// Converts an Action to a Task
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static Func<T, Task> AsTask<T>(this Action<T>? callback) => p => {
        callback?.Invoke(p);
        return Task.CompletedTask;
    };
}