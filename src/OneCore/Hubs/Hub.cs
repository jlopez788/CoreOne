namespace OneCore.Hubs;

public sealed class Hub : IHub
{
    public static readonly Hub Global = new(Guid.Empty);
    private static readonly List<Hub> Instances = [];
    private readonly Guid Id;
    private readonly DataList<Type, IHubMessageIntercept> Intercepts = [];
    private readonly DataList<Type, IHubSubscription> Subscriptions = [];
    private readonly object Sync = new();

    public Hub()
    {
        Id = Guid.NewGuid();
        Instances.Add(this);
    }

    private Hub(Guid id) => Id = id;

    public void Dispose()
    {
        if (Id == Guid.Empty)
        {
            Instances.Each(p => p.Dispose());
            Instances.Clear();
        }

        lock (Sync)
        {
            Intercepts.Clear();
            Subscriptions.Clear();
            Instances.Remove(this);
        }

        GC.SuppressFinalize(this);
    }

    public void Intercept<TEvent>(InterceptHubMessage<TEvent> onintercept, int order, CancellationToken token) where TEvent : IHubMessage
    {
        if (onintercept is null)
            return;

        var key = typeof(TEvent);
        var sub = new MessageIntercept<TEvent>(onintercept, order);
        lock (Sync)
            Intercepts.Add(key, sub);

        token.Register(() => {
            lock (Sync)
            {
                if (Intercepts.TryGetValue(key, out var current))
                {
                    current?.Remove(sub);
                    if (current is not null)
                        Intercepts.Set(key, current);
                }
            }
        });
    }

    public HubPublish<TEvent> Publish<TEvent>(TEvent message) where TEvent : IHubMessage
    {
        if (message is IGlobalHubMessage gmessage && gmessage.IsGlobal)
        {
            return PublishGlobal(message);
        }

        var task = Task.Factory.StartNew(async () => {
            await PublishInternal(message);
            return message;
        }).Unwrap();

        return new HubPublish<TEvent>(task);
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> onmessage, CancellationToken token, Predicate<TEvent>? messageFilter = null) where TEvent : IHubMessage
    {
        if (onmessage == null)
            return;

        var key = typeof(TEvent);
        var subscription = new MessageSubscription<TEvent>(onmessage, messageFilter);
        lock (Sync)
            Subscriptions.Add(key, subscription);

        token.Register(() => {
            lock (Sync)
            {
                if (Subscriptions.TryGetValue(key, out var current))
                {
                    current?.Remove(subscription);
                    Subscriptions.Set(key, current);
                }
            }
        });
    }

    private static HubPublish<TEvent> PublishGlobal<TEvent>(TEvent message) where TEvent : IHubMessage
    {
        List<Hub>? targets = null;
        lock (Global.Sync)
        {
            targets = [.. Instances];
        }

        return new HubPublish<TEvent>(Task.Factory.StartNew(async () => {
            await targets.EachAsync(p => p.PublishInternal(message));
            return message;
        }).Unwrap());
    }

    private void PublishExceptionMsg<T>(Exception ex) => Publish(new ExceptionMessage(ex, $"Unable to deliver message: {typeof(T).FullName}"));

    private async Task PublishInternal<TEvent>(TEvent message) where TEvent : IHubMessage
    {
        if (message is null)
        {
            return;
        }

        var key = typeof(TEvent);
        var msgType = message.GetType();
        HashSet<IHubSubscription> hashset = [];
        HashSet<IHubMessageIntercept> intercepts = [];
        lock (Sync)
        {
            intercepts = [.. Intercepts.Where(kp => inherits(kp.Key))
                .SelectMany(kp => kp.Value ?? [])
                .ExcludeNulls()
                .OrderBy(p => p.Order)];
            hashset = [.. Subscriptions.Where(kp => inherits(kp.Key))
                .SelectMany(kp => kp.Value ?? [])
                .ExcludeNulls()];
        }
        try
        {
            var ok = Result.Ok;
            var fail = Result.Fail();
            var exceptions = new List<Exception>();
            var result = await intercepts.AggregateResultAsync(ok, async (next, intercept) => {
                var t = await intercept.Intercept(message);
                return t == ResultType.Success ? ok : fail;
            });
            if (result.Success)
            {
                foreach (var p in hashset)
                {
                    var ex = await deliverMsg(p);
                    if (ex is not null)
                        exceptions.Add(ex);
                }
                if (exceptions.Count > 0)
                    PublishExceptionMsg<TEvent>(new AggregateException(exceptions));
            }
        }
        catch (Exception ex)
        {
            PublishExceptionMsg<TEvent>(ex);
        }

        bool inherits(Type type) => msgType.IsSubclassOf(type) || key.IsSubclassOf(type) || msgType == type || type == key;
        async Task<Exception?> deliverMsg(IHubSubscription sub)
        {
            try
            { await sub.Deliver(message); }
            catch (Exception ex) { return ex; }
            return null;
        }
    }
}