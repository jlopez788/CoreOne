using CoreOne;
using CoreOne.Services;

namespace Tests.Services;

public class DelayedBusyStateTests
{
    [Test]
    public void ZeroDelay_ShowsAndHidesImmediately()
    {
        var token = SToken.Create();
        var changes = new List<bool>();
        using var state = new DelayedBusyState();
        state.Subscribe(changes.Add, token);

        Assert.That(state.SetActive(true), Is.True);
        Assert.That(state.IsVisible, Is.True);
        Assert.That(state.SetActive(false), Is.True);
        Assert.That(state.IsVisible, Is.False);
        Assert.That(changes, Is.EqualTo(new[] { true, false }));
    }

    [Test]
    public async Task StopBeforeDelay_NeverBecomesVisible()
    {
        var token = SToken.Create();
        var changes = new List<bool>();
        using var state = new DelayedBusyState(TimeSpan.FromMilliseconds(150));
        state.Subscribe(changes.Add, token);

        Assert.That(state.SetActive(true), Is.False);
        await Task.Delay(30);
        Assert.That(state.SetActive(false), Is.False);
        await Task.Delay(250);

        Assert.That(state.IsVisible, Is.False);
        Assert.That(changes, Is.Empty);
    }

    [Test]
    public async Task ActiveLongerThanDelay_BecomesVisible()
    {
        var token = SToken.Create();
        var changes = new List<bool>();
        using var state = new DelayedBusyState(TimeSpan.FromMilliseconds(50));
        state.Subscribe(changes.Add, token);

        state.SetActive(true);
        Assert.That(state.IsVisible, Is.False);
        await Task.Delay(200);

        Assert.That(state.IsVisible, Is.True);
        Assert.That(state.SetActive(false), Is.True);
        Assert.That(state.IsVisible, Is.False);
        Assert.That(changes, Is.EqualTo(new[] { true, false }));
    }

    [Test]
    public async Task MinVisible_DelaysHide()
    {
        var token = SToken.Create();
        var changes = new List<bool>();
        using var state = new DelayedBusyState(TimeSpan.Zero, TimeSpan.FromMilliseconds(200));
        state.Subscribe(changes.Add, token);

        state.SetActive(true);
        Assert.That(state.SetActive(false), Is.False);
        Assert.That(state.IsVisible, Is.True);
        Assert.That(state.IsActive, Is.False);

        await Task.Delay(400);
        Assert.That(state.IsVisible, Is.False);
        Assert.That(changes, Is.EqualTo(new[] { true, false }));
    }

    [Test]
    public async Task Reactivated_DuringPendingHide_StaysVisible()
    {
        var token = SToken.Create();
        var changes = new List<bool>();
        using var state = new DelayedBusyState(TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        state.Subscribe(changes.Add, token);

        state.SetActive(true);
        state.SetActive(false);
        state.SetActive(true);
        await Task.Delay(250);

        Assert.That(state.IsVisible, Is.True);
        Assert.That(changes, Is.EqualTo(new[] { true }));
    }

    [Test]
    public async Task Dispose_CancelsPendingShow()
    {
        var token = SToken.Create();
        var changes = new List<bool>();
        var state = new DelayedBusyState(TimeSpan.FromMilliseconds(50));
        state.Subscribe(changes.Add, token);

        state.SetActive(true);
        state.Dispose();
        await Task.Delay(150);

        Assert.That(changes, Is.Empty);
    }
}