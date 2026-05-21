namespace CoreOne.Interceptors;

public class LogInterceptor(OLog<LogInterceptor> logger) : IAsyncInterceptor
{
    public async Task<object?> InterceptAsync(IInvocation invocation)
    {
        try
        {
            return await invocation.ProceedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception in {Method}: {Message}", invocation.Method.Name, ex.InnerException?.Message ?? ex.Message);

            var returnType = invocation.GetReturnType();
            var genericDefinition = returnType.IsGenericType ? returnType.GetGenericTypeDefinition() : null;
            if (genericDefinition == Types.IResultT || genericDefinition == Types.ResultT)
            {
                var msg = $"Exception in {invocation.Method.Name}: {ex.InnerException?.Message ?? ex.Message}";
                if (genericDefinition == Types.ResultT)
                {
                    var errorResult = Activator.CreateInstance(returnType, [invocation.GetDefaultReturnValue(), msg, ResultType.Exception]);
                    return errorResult;
                }
                else if (genericDefinition == Types.IResultT)
                { // For IResult<T>, we can create a Result<T> instance and return it as IResult<T>
                    var resultType = returnType.GetGenericArguments()[0];
                    var errorResultType = typeof(Result<>).MakeGenericType(resultType);
                    var errorResult = Activator.CreateInstance(errorResultType, [invocation.GetDefaultReturnValue(), msg, ResultType.Exception]);
                    return errorResult;
                }
            }
            else if (returnType == Types.IResult || returnType == Types.Result)
            {
                var msg = $"Exception in {invocation.Method.Name}: {ex.InnerException?.Message ?? ex.Message}";
                if (returnType == Types.Result)
                {
                    var errorResult = Activator.CreateInstance(returnType, [ResultType.Exception, msg]);
                    return errorResult;
                }
                else if (returnType == Types.IResult)
                {
                    var errorResult = Activator.CreateInstance(Types.Result, [ResultType.Exception, msg]);
                    return errorResult;
                }
            }

            return invocation.GetDefaultReturnValue();
        }
    }
}