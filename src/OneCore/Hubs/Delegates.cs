namespace CoreOne.Hubs;

public delegate Task<ResultType> InterceptHubMessage<TEvent>(TEvent message);