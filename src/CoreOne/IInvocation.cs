namespace CoreOne;

public interface IInvocation
{
    object[] Arguments { get; }
    MethodInfo Method { get; }
    string MethodName { get; }
    Func<Task<object?>> ProceedAsync { get; }
}