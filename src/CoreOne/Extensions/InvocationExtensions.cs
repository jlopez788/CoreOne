namespace CoreOne.Extensions;

public static class InvocationExtensions
{
    public static object? GetDefaultReturnValue(this IInvocation invocation)
    {
        var returnType = invocation.Method.ReturnType;
        if (returnType == typeof(void))
            return null;

        if (returnType == typeof(Task))
            return Task.CompletedTask;

        if (returnType == typeof(ValueTask))
            return new ValueTask();

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var innerType = returnType.GetGenericArguments()[0];
            var defaultValue = innerType.IsValueType ? Activator.CreateInstance(innerType) : null;
            return defaultValue;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var innerType = returnType.GetGenericArguments()[0];
            var defaultValue = innerType.IsValueType ? Activator.CreateInstance(innerType) : null;
            return defaultValue;
        }

        return returnType.IsValueType ? Activator.CreateInstance(returnType)! : null;
    }

    public static Type GetReturnType(this IInvocation invocation)
    {
        var returnType = invocation.Method.ReturnType;
        return returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>) ?
            returnType.GetGenericArguments()[0] :
            returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>) ?
            returnType.GetGenericArguments()[0] : returnType;
    }
}