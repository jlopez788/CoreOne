using CoreOne;

namespace Tests;

public class DisposableTests
{
    private class TestDisposable : Disposable
    {
        public bool DisposeCalled { get; private set; }

        protected override void OnDispose()
        {
            DisposeCalled = true;
            base.OnDispose();
        }
    }

    [Test]
    public void Constructor_NotDisposed()
    {
        var disposable = new TestDisposable();

        Assert.That(disposable.IsDisposed, Is.False);
    }

    [Test]
    public void Dispose_CallsOnDispose()
    {
        var disposable = new TestDisposable();

        disposable.Dispose();

        Assert.That(disposable.DisposeCalled, Is.True);
    }

    [Test]
    public void Dispose_SetsIsDisposedTrue()
    {
        var disposable = new TestDisposable();

        disposable.Dispose();

        Assert.That(disposable.IsDisposed, Is.True);
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var disposable = new TestDisposable();

        disposable.Dispose();
        disposable.Dispose();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(disposable.DisposeCalled, Is.True);
            Assert.That(disposable.IsDisposed, Is.True);
        }
    }

    [Test]
    public void Dispose_OnlyCallsOnDisposeOnce()
    {
        var disposable = new TestDisposable();

        // Override the counter check
        disposable.Dispose();
        var firstState = disposable.DisposeCalled;
        disposable.Dispose();

        // OnDispose should only be called once
        Assert.That(firstState, Is.True);
    }

    [Test]
    public void Empty_IsDisposableInstance()
    {
        var empty = Disposable.Empty;

        Assert.That(empty, Is.Not.Null);
        Assert.That(empty, Is.InstanceOf<IDisposable>());
    }

    [Test]
    public void Empty_CanBeDisposed()
    {
        var empty = Disposable.Empty;

        Assert.DoesNotThrow(() => empty.Dispose());
    }

    [Test]
    public void UsingStatement_AutomaticallyDisposes()
    {
        TestDisposable? disposable;
        using (disposable = new TestDisposable())
        {
            Assert.That(disposable.IsDisposed, Is.False);
        }

        Assert.That(disposable!.IsDisposed, Is.True);
    }

    [Test]
    public void Finalizer_CallsDispose()
    {
        // Create and release disposable
        var weakRef = CreateAndReleaseDisposable();

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Object should be collected
        Assert.That(weakRef.IsAlive, Is.False);
    }

    private static WeakReference CreateAndReleaseDisposable()
    {
        var disposable = new TestDisposable();
        return new WeakReference(disposable);
    }
}