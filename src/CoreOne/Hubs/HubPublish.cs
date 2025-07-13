namespace CoreOne.Hubs;

public sealed class HubPublish<TEvent>(Task<TEvent> task)
{
    public void OnComplete(Func<TEvent, Task> oncomplete) => task.ContinueWith(async p => {
        var result = await p;
        await oncomplete.Invoke(result);
    });
}