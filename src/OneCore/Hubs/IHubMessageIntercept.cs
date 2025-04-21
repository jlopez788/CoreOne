namespace CoreOne.Hubs;

internal interface IHubMessageIntercept
{
    int Order { get; }

    Task<ResultType> Intercept(IHubMessage message);
}
