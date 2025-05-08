namespace CoreOne.Hubs;

internal record StateMessage<T>(StateKey Key, T? Model = default) : IStateMessage;