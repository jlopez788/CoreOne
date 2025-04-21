namespace CoreOne.Hubs;

internal class MessageIntercept<TEvent>(InterceptHubMessage<TEvent> onintercept, int order) : IHubMessageIntercept where TEvent : IHubMessage
{
    public int Order { get; } = order;
    protected InterceptHubMessage<TEvent> OnIntercept { get; } = onintercept;

    public async Task<ResultType> Intercept(IHubMessage message)
    {
        var result = ResultType.Success;
        var task = OnIntercept?.Invoke((TEvent)message);
        if (task is not null && !task.IsCompleted)
            result = await task;
        return result;
    }
}
