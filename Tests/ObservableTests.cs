using CoreOne;
using CoreOne.Reactive;
using Moq;
using System.ComponentModel;

namespace Tests;

public class ObservableTests
{
    private class TestEvent
    {
        public event EventHandler<EventArgs>? Event;
        public event EventHandler<CancelEventArgs>? CancelEvent;

        public void RaiseEvent() => Event?.Invoke(this, EventArgs.Empty);

        public void RaiseCancelEvent(CancelEventArgs args) => CancelEvent?.Invoke(this, args);
    }

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test_MoqAction_NotTriggered_IfRaiseNotCalled()
    {
        var target = new TestEvent();
        var token = SToken.Create();
        var observer = Observable.FromEvent<EventArgs>(target, nameof(target.Event));
        Assert.That(observer, Is.Not.Null);

        var actionMock = new Mock<Action<EventArgs>>();
        observer.Subscribe(actionMock.Object, token);

        // Do NOT call target.RaiseEvent()

        // Verify the delegate was never invoked
        actionMock.Verify(m => m.Invoke(It.IsAny<EventArgs>()), Times.Never());
    }

    [Test]
    public void Test_ObserverNotCalled_WhenTokenDisposed()
    {
        var target = new TestEvent();
        var token = SToken.Create();
        var observer = Observable.FromEvent<EventArgs>(target, nameof(target.Event));
        Assert.That(observer, Is.Not.Null);

        var actionMock = new Mock<Action<EventArgs>>();
        observer.Subscribe(actionMock.Object, token);

        // Dispose the token before raising the event so the subscription should be removed
        token.Dispose();

        target.RaiseEvent();

        actionMock.Verify(m => m.Invoke(It.IsAny<EventArgs>()), Times.Never());
    }

    [Test]
    public void Test_CancelEvent_ObserverCalled()
    {
        var target = new TestEvent();
        using var token = SToken.Create();
        var observer = Observable.FromEvent<CancelEventArgs>(target, nameof(target.CancelEvent));
        Assert.That(observer, Is.Not.Null);

        var args = new CancelEventArgs();
        var actionMock = new Mock<Action<CancelEventArgs>>();
        observer.Subscribe(actionMock.Object, token);
        target.RaiseCancelEvent(args);
        actionMock.Verify(m => m.Invoke(It.IsAny<CancelEventArgs>()), Times.Once());
    }

    public void TestCancelEventCanceled()
    {
        var target = new TestEvent();
        using var token = SToken.Create();
        var observer = Observable.FromEvent<CancelEventArgs>(target, nameof(target.CancelEvent));
        Assert.That(observer, Is.Not.Null);

        var args = new CancelEventArgs();
        observer.Subscribe(p => p.Cancel = true, token);
        target.RaiseCancelEvent(args);
        Assert.That(args.Cancel, Is.True);
    }

    [Test]
    public void BasicTest()
    {
        var target = new TestEvent();
        using var token = SToken.Create();
        var observer = Observable.FromEvent<EventArgs>(target, nameof(target.Event));
        Assert.That(observer, Is.Not.Null);

        var actionMock = new Mock<Action<EventArgs>>();
        observer.Subscribe(actionMock.Object, token);

        target.RaiseEvent();
        actionMock.Verify(m => m.Invoke(It.IsAny<EventArgs>()), Times.Once());
    }
}