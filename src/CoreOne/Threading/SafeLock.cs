namespace CoreOne.Threading;

public class SafeLock
{
#if NET9_0_OR_GREATER
    private readonly Lock Sync = new();
#else
    private readonly object _Sync = new object();
#endif

    public IDisposable EnterScope()
    {
#if NET9_0_OR_GREATER
        Sync.Enter();
        return new Subscription(Sync.Exit);
#else
       Monitor.Enter(_Sync);
       return new Subscription(() => Monitor.Exit(_Sync));  
#endif
    }
}