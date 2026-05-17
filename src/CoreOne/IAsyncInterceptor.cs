namespace CoreOne;

public interface IAsyncInterceptor
{
    Task<object?> InterceptAsync(IInvocation invocation);
}