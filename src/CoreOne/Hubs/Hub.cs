namespace CoreOne.Hubs;

public class Hub : Disposable, IHub
{
    private sealed class GlobalHub() : Hub(Guid.Empty)
    {
        protected override void OnDispose()
        {
            using (Sync.EnterScope())
            {
                Instances.Each(p => p.Dispose());
                Instances.Clear();
            }

            base.OnDispose();
        }

        public override string ToString() => $"Global - {Subscriptions.Count}";
    }

    public static readonly Hub Global = new GlobalHub();
    private static readonly List<Hub> Instances = [];
    private readonly Guid Id;
    private readonly DataList<Type, IHubMessageIntercept> Intercepts = [];
    private readonly AsyncTaskQueue Queue = new();
    private readonly Data<StateKey, IStateMessage> States = [];
    private readonly DataList<Type, IHubSubscription> Subscriptions = [];
    protected readonly SafeLock Sync = new();

    public Hub()
    {
        Id = ID.Create();
        Instances.Add(this);
    }

    protected Hub(Guid id) => Id = id;

    protected override void OnDispose()
    {
        base.OnDispose();

        using (Sync.EnterScope())
        {
            Intercepts.Clear();
            Subscriptions.Clear();
            Instances.Remove(this);
        }
    }

#if NET9_0_OR_GREATER

    public TState GetState<TState>(string? name = null) where TState : IHubState<TState>
    {
        var key = StateKey.Create<TState>(name);
        return States.TryGetValue(key, out var value) && value is StateMessage<TState> current ?
            current.Model ?? TState.Default : TState.Default;
    }

#else

    public TState? GetState<TState>(string? name = null) where TState : IHubState<TState>
    {
        var key = StateKey.Create<TState>(name);
        return States.TryGetValue(key, out var value) && value is StateMessage<TState> current ?
            current.Model ?? default : default;
    }

#endif

    public void Intercept<TEvent>(InterceptHubMessage<TEvent> onintercept, int order, CancellationToken token)
    {
        if (onintercept is null)
            return;

        var key = typeof(TEvent);
        var sub = new MessageIntercept<TEvent>(onintercept, order);
        using (Sync.EnterScope())
            Intercepts.Add(key, sub);

        token.Register(() => {
            using (Sync.EnterScope())
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

    public HubPublish<TEvent> Publish<TEvent>(TEvent message)
    {
        return message is IGlobalHubMessage gmessage && gmessage.IsGlobal ?
            PublishGlobal(message) :
            new HubPublish<TEvent>(Queue.Enqueue(async () => {
                await PublishInternal(message);
                return message;
            }));
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> onmessage, Predicate<TEvent>? filter, CancellationToken cancellationToken)
    {
        if (onmessage is null)
            return;
        bool implementsHubState = typeof(TEvent).Implements(typeof(IHubState<>));
        if (implementsHubState)
        {
            var method = typeof(Hub).GetMethod(nameof(SubscribeState), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            method = method?.MakeGenericMethod(typeof(TEvent));
            var callback = MetaType.GetInvokeMethod(method);
            callback.Invoke(this, [null, onmessage, cancellationToken, filter]);
            return;
        }

        var key = typeof(TEvent);
        var subscription = new MessageSubscription<TEvent>(onmessage, filter);
        cancellationToken.Register(RegisterSubscription(subscription, typeof(TEvent)));
    }

    public void SubscribeState<TEvent>(string? name, Func<TEvent, Task> onstate, Predicate<TEvent>? filter, CancellationToken cancellationToken) where TEvent : IHubState<TEvent>
    {
        var key = StateKey.Create<TEvent>(name);
        var subscription = new StateMessageSubscription<TEvent>(name, onstate, filter);
        cancellationToken.Register(RegisterSubscription(subscription, typeof(TEvent)));
        if (TryGetState<TEvent>(name, out var state))
            _ = subscription.Deliver(state);
    }

    private void PublishExceptionMsg<T>(Exception ex) => Publish(new ExceptionMessage(ex, $"Unable to deliver message: {typeof(T).FullName}"));

    private HubPublish<TEvent> PublishGlobal<TEvent>(TEvent message)
    {
        List<Hub>? targets = null;
        using (Global.Sync.EnterScope())
        {
            targets = [.. Instances];
        }

        return new HubPublish<TEvent>(Queue.Enqueue(async () => {
            await targets.EachAsync(p => p.PublishInternal(message));
            return message;
        }));
    }

    private async Task PublishInternal<TEvent>(TEvent message)
    {
        if (message is null)
        {
            return;
        }

        if (message is IHubState<TEvent> state)
        {
            var stateKey = StateKey.Create<TEvent>(state.Name);
            var stateMsg = new StateMessage<TEvent>(stateKey, message);
            States.Set(stateKey, stateMsg);
        }

        var key = typeof(TEvent);
        var msgType = message.GetType();
        HashSet<IHubSubscription> hashset = [];
        HashSet<IHubMessageIntercept> intercepts = [];
        using (Sync.EnterScope())
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
                var t = await intercept.Intercept((IHubMessage)message);
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
            { await sub.Deliver((IHubMessage)message); }
            catch (Exception ex) { return ex; }
            return null;
        }
    }

    private Action RegisterSubscription(IHubSubscription subscription, Type key)
    {
        using (Sync.EnterScope())
        {
            Subscriptions.Add(key, subscription);
        }

        return (() => {
            using (Sync.EnterScope())
            {
                if (Subscriptions.TryGetValue(key, out var current))
                {
                    current?.Remove(subscription);
                    Subscriptions.Set(key, current);
                }
            }
        });
    }

    private bool TryGetState<T>(string? name, [NotNullWhen(true)] out T? state)
    {
        var key = StateKey.Create<T>(name);
        state = default;
        if (States.TryGetValue(key, out var value) && value is StateMessage<T> current)
            state = current.Model;

        return state is not null;
    }

    public override string ToString() => $"{Id.ToShortId()} - {Subscriptions.Count}";
}