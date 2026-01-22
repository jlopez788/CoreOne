namespace CoreOne.Hubs;

internal class MessageIntercept<TEvent>(InterceptHubMessage<TEvent>? onintercept, int order) : IHubMessageIntercept
{
    public int Order { get; } = order;

    public async Task<ResultType> Intercept(object message)
    {
        var result = ResultType.Success;
        var task = onintercept?.Invoke((TEvent)message);
        if (task is not null && !task.IsCompleted)
            result = await task;
        return result;
    }
}