using CoreOne;
using CoreOne.Services;

namespace Tests.Services;

public class LoadingStoreTests
{
    [Test]
    public void Default_IsBusyImmediately()
    {
        using var store = new LoadingStore();
        var changes = new List<bool>();
        using var token = SToken.Create();
        store.Subscribe(changes.Add, token);

        var lock1 = store.MarkBusy();
        Assert.Multiple(() => {
            Assert.That(store.IsBusy, Is.True);
            Assert.That(store.IsWorking, Is.True);
        });

        lock1.Dispose();
        Assert.That(store.IsBusy, Is.False);
        Assert.That(changes, Is.EqualTo(new[] { true, false }));
    }

    [Test]
    public async Task Delayed_FastWork_NeverBusy_ButNotifiesCompletion()
    {
        using var store = new LoadingStore(TimeSpan.FromMilliseconds(150));
        var changes = new List<bool>();
        using var token = SToken.Create();
        store.Subscribe(changes.Add, token);

        await store.InvokeAsync(async () => await Task.Delay(20), default);
        await Task.Delay(250);

        Assert.That(store.IsBusy, Is.False);
        Assert.That(store.IsWorking, Is.False);
        Assert.That(changes, Does.Not.Contain(true));
        Assert.That(changes, Is.Not.Empty);
    }

    [Test]
    public async Task Delayed_SlowWork_BecomesBusyAfterDelay()
    {
        using var store = new LoadingStore(TimeSpan.FromMilliseconds(50));
        var busy = store.MarkBusy();

        Assert.That(store.IsWorking, Is.True);
        Assert.That(store.IsBusy, Is.False);
        await Task.Delay(200);
        Assert.That(store.IsBusy, Is.True);

        busy.Dispose();
        Assert.That(store.IsBusy, Is.False);
        Assert.That(store.IsWorking, Is.False);
    }

    [Test]
    public async Task Configure_AppliesMinBusyDuration()
    {
        using var store = new LoadingStore();
        store.Configure(TimeSpan.Zero, TimeSpan.FromMilliseconds(150));

        store.MarkBusy().Dispose();
        Assert.That(store.IsBusy, Is.True);
        Assert.That(store.IsWorking, Is.False);

        await Task.Delay(300);
        Assert.That(store.IsBusy, Is.False);
    }
}
