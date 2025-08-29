using System.Runtime.CompilerServices;

namespace CoreOne.Threading.Tasks;

public class SafeTask
{
    private readonly TaskCompletionSource<object?> Task;

    public SafeTask(object? result)
    {
        Task = new();
        if (result is Task task)
        {
            task.ContinueWith(p => {
                try
                {
                    var meta = MetaType.GetMetadata(p.GetType(), nameof(Result));
                    if (meta != Metadata.Empty)
                        Task.SetResult(meta.GetValue(p));
                    else
                        Task.SetResult(null);
                }
                catch (Exception ex)
                {
                    Task.SetException(ex);
                }
            });
        }
        else
            Task.SetResult(result);
    }

    public TaskAwaiter<object?> GetAwaiter() => Task.Task.GetAwaiter();
}