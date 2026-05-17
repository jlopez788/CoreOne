namespace CoreOne.Models;

public class Invocation : IInvocation
{
    public string MethodName { get; init; } = string.Empty;
    public MethodInfo Method { get; init; } = default!;
    public object[] Arguments { get; init; } = [];
    public Func<Task<object?>> ProceedAsync { get; init; } = default!;
}