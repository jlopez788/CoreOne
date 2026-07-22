namespace CoreOne;

public delegate Task InvokeATask<TValue>(TValue value, CancellationToken cancellationToken);

public delegate Task InvokeTask(CancellationToken cancellationToken);

public delegate ValueTask<TResult> InvokeTask<TResult>(CancellationToken cancellationToken);

public delegate void InvokeTaskAsync(InvokeTask task);