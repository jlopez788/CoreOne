namespace CoreOne.Hubs;

public sealed record HubPublish<TEvent>
{
    private readonly Task<TEvent> Task;

    public HubPublish(Task<TEvent> task) => Task = task;

    public void OnComplete(Func<TEvent, Task> oncomplete) => Task.ContinueWith(async p => {
        var result = await p;
        await oncomplete.Invoke(result);
    });
}
