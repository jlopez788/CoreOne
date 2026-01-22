using CoreOne;
using CoreOne.Reactive;
using CoreOne.Hubs;
using CoreOne.Extensions;
using Moq;
using System.ComponentModel;

namespace Tests;

public class ObservableTests
{
    public class TestEvent
    {
        public event EventHandler<EventArgs>? Event;
        public event EventHandler<CancelEventArgs>? CancelEvent;
        public event EventHandler<PropertyChangedEventArgs>? PropertyChanged;

        public void RaiseEvent() => Event?.Invoke(this, EventArgs.Empty);

        public void RaiseCancelEvent(CancelEventArgs args) => CancelEvent?.Invoke(this, args);

        public void RaisePropertyChanged(string propertyName) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class TestMessage { public int Value { get; set; } }

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

    // Subject Tests
    [Test]
    public void Subject_Subscribe_ReceivesOnNext()
    {
        var subject = new Subject<int>();
        var received = new List<int>();
        using var token = SToken.Create();
        
        subject.Subscribe(received.Add, token);
        
        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);
        
        Assert.That(received, Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(subject.HasObservers, Is.True);
    }

    [Test]
    public void Subject_OnCompleted_NotifiesObservers()
    {
        var subject = new Subject<int>();
        var completed = false;
        using var token = SToken.Create();
        
        subject.Subscribe(_ => { }, () => completed = true, token);
        
        subject.OnCompleted();
        
        Assert.That(completed, Is.True);
        Assert.That(subject.HasObservers, Is.False);
    }

    [Test]
    public void Subject_OnError_NotifiesObservers()
    {
        var subject = new Subject<int>();
        Exception? receivedException = null;
        var observer = new Mock<IObserver<int>>();
        observer.Setup(o => o.OnError(It.IsAny<Exception>()))
            .Callback<Exception>(ex => receivedException = ex);
        
        subject.Subscribe(observer.Object);
        
        var testException = new InvalidOperationException("test");
        subject.OnError(testException);
        
        Assert.That(receivedException, Is.EqualTo(testException));
        observer.Verify(o => o.OnError(testException), Times.Once);
        Assert.That(subject.HasObservers, Is.False);
    }

    [Test]
    public void Subject_AfterCompleted_DoesNotSendOnNext()
    {
        var subject = new Subject<int>();
        var received = new List<int>();
        using var token = SToken.Create();
        
        subject.Subscribe(received.Add, token);
        subject.OnNext(1);
        subject.OnCompleted();
        subject.OnNext(2); // Should not be received
        
        Assert.That(received, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void Subject_Dispose_CallsOnCompleted()
    {
        var subject = new Subject<int>();
        var completed = false;
        using var token = SToken.Create();
        
        subject.Subscribe(_ => { }, () => completed = true, token);
        
        subject.Dispose();
        
        Assert.That(completed, Is.True);
    }

    [Test]
    public void Subject_Unsubscribe_StopsReceivingNotifications()
    {
        var subject = new Subject<int>();
        var received = new List<int>();
        
        var subscription = subject.Subscribe(Observer.Create<int>(received.Add));
        
        subject.OnNext(1);
        subscription.Dispose();
        subject.OnNext(2); // Should not be received
        
        Assert.That(received, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void Subject_SubscribeWithNull_ReturnsEmptyDisposable()
    {
        var subject = new Subject<int>();
        var subscription = subject.Subscribe(null!);
        
        Assert.That(subscription, Is.Not.Null);
        Assert.DoesNotThrow(() => subscription.Dispose());
    }

    // BehaviorSubject Tests
    [Test]
    public void BehaviorSubject_NewSubscriber_ReceivesCurrentValue()
    {
        var subject = new BehaviorSubject<int>(42);
        var received = new List<int>();
        using var token = SToken.Create();
        
        subject.Subscribe(received.Add, token);
        
        Assert.That(received, Is.EqualTo(new[] { 42 }));
    }

    [Test]
    public void BehaviorSubject_Value_ReturnsCurrentValue()
    {
        var subject = new BehaviorSubject<int>(10);
        
        Assert.That(subject.Value, Is.EqualTo(10));
        
        subject.OnNext(20);
        
        Assert.That(subject.Value, Is.EqualTo(20));
    }

    [Test]
    public void BehaviorSubject_TryGetValue_ReturnsTrue()
    {
        var subject = new BehaviorSubject<string>("test");
        
        var success = subject.TryGetValue(out var value);
        
        Assert.That(success, Is.True);
        Assert.That(value, Is.EqualTo("test"));
    }

    [Test]
    public void BehaviorSubject_Disposed_TryGetValue_ReturnsFalse()
    {
        var subject = new BehaviorSubject<string>("test");
        subject.Dispose();
        
        var success = subject.TryGetValue(out var value);
        
        Assert.That(success, Is.False);
        Assert.That(value, Is.Null);
    }

    [Test]
    public void BehaviorSubject_AfterError_ValueThrows()
    {
        var subject = new BehaviorSubject<int>(10);
        var testException = new InvalidOperationException("error");
        
        subject.OnError(testException);
        
        Assert.Throws<InvalidOperationException>(() => _ = subject.Value);
    }

    [Test]
    public void BehaviorSubject_AfterError_TryGetValueThrows()
    {
        var subject = new BehaviorSubject<int>(10);
        var testException = new InvalidOperationException("error");
        
        subject.OnError(testException);
        
        Assert.Throws<InvalidOperationException>(() => subject.TryGetValue(out _));
    }

    [Test]
    public void BehaviorSubject_Dispose_ClearsValue()
    {
        var subject = new BehaviorSubject<string>("test");
        
        subject.Dispose();
        
        var success = subject.TryGetValue(out _);
        Assert.That(success, Is.False);
    }

    // Observable.Where Tests
    [Test]
    public void Observable_Where_FiltersValues()
    {
        var subject = new Subject<int>();
        var received = new List<int>();
        using var token = SToken.Create();
        
        subject.Where(x => x > 5).Subscribe(received.Add, token);
        
        subject.OnNext(3);
        subject.OnNext(7);
        subject.OnNext(2);
        subject.OnNext(10);
        
        Assert.That(received, Is.EqualTo(new[] { 7, 10 }));
    }

    // Observable.Select Tests
    [Test]
    public void Observable_Select_TransformsValues()
    {
        var subject = new Subject<int>();
        var received = new List<string>();
        using var token = SToken.Create();
        
        subject.Select(x => x.ToString()).Subscribe(received.Add, token);
        
        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);
        
        Assert.That(received, Is.EqualTo(new[] { "1", "2", "3" }));
    }

    [Test]
    public async Task Observable_SelectAsync_TransformsValues()
    {
        var subject = new Subject<int>();
        var received = new List<string>();
        var tcs = new TaskCompletionSource<bool>();
        using var token = SToken.Create();
        
        subject.Select(async x => {
            await Task.Delay(10);
            return x.ToString();
        }).Subscribe(x => {
            received.Add(x);
            if (received.Count == 3)
                tcs.TrySetResult(true);
        }, token);
        
        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);
        
        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        // Async operations may complete out of order
        Assert.That(received.Count, Is.EqualTo(3));
        Assert.That(received, Does.Contain("1"));
        Assert.That(received, Does.Contain("2"));
        Assert.That(received, Does.Contain("3"));
    }

    [Test]
    public async Task Observable_SelectAsync_WithError_CallsOnError()
    {
        var subject = new Subject<int>();
        Exception? receivedError = null;
        var tcs = new TaskCompletionSource<bool>();
        
        var observer = Observer.Create<string>(
            onNext: _ => { },
            onError: ex => {
                receivedError = ex;
                tcs.TrySetResult(true);
            });
        
        subject.Select(async x => {
            await Task.Delay(10);
            throw new InvalidOperationException("test error");
#pragma warning disable CS0162
            return x.ToString();
#pragma warning restore CS0162
        }).Subscribe(observer);
        
        subject.OnNext(1);
        
        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        Assert.That(receivedError, Is.Not.Null);
        // Error is wrapped in AggregateException by Task.ContinueWith
        Assert.That(receivedError, Is.InstanceOf<AggregateException>());
        var aggEx = (AggregateException)receivedError;
        Assert.That(aggEx.InnerException, Is.InstanceOf<InvalidOperationException>());
    }

    // Observable.Distinct Tests
    [Test]
    public void Observable_Distinct_RemovesConsecutiveDuplicates()
    {
        var subject = new Subject<int>();
        var received = new List<int>();
        using var token = SToken.Create();
        
        subject.Distinct().Subscribe(received.Add, token);
        
        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(2);
        subject.OnNext(3);
        subject.OnNext(3);
        subject.OnNext(3);
        
        // Distinct only prevents consecutive duplicates (backed by BackingField)
        Assert.That(received, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Observable_DistinctWithKeySelector_RemovesConsecutiveDuplicatesByKey()
    {
        var subject = new Subject<TestMessage>();
        var received = new List<TestMessage>();
        using var token = SToken.Create();
        
        subject.Distinct(m => m.Value).Subscribe(received.Add, token);
        
        var msg1 = new TestMessage { Value = 1 };
        var msg2 = new TestMessage { Value = 2 };
        var msg3 = new TestMessage { Value = 2 }; // Consecutive duplicate key
        var msg4 = new TestMessage { Value = 3 };
        
        subject.OnNext(msg1);
        subject.OnNext(msg2);
        subject.OnNext(msg3);
        subject.OnNext(msg4);
        
        Assert.That(received.Count, Is.EqualTo(3));
        Assert.That(received[0], Is.SameAs(msg1));
        Assert.That(received[1], Is.SameAs(msg2));
        Assert.That(received[2], Is.SameAs(msg4));
    }

    // Observable.Throttle Tests
    [Test]
    public async Task Observable_Throttle_DebouncesFastValues()
    {
        var subject = new Subject<int>();
        var received = new List<int>();
        var tcs = new TaskCompletionSource<bool>();
        using var token = SToken.Create();
        
        subject.Throttle(100).Subscribe(x => {
            received.Add(x);
            tcs.TrySetResult(true);
        }, token);
        
        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);
        await Task.Delay(50);
        subject.OnNext(4);
        subject.OnNext(5);
        
        await Task.WhenAny(tcs.Task, Task.Delay(500));
        
        // Should only receive the last value after debounce period
        Assert.That(received.Count, Is.EqualTo(1));
        Assert.That(received[0], Is.EqualTo(5));
    }

    [Test]
    public async Task Observable_ThrottleWithTimeSpan_DebouncesFastValues()
    {
        var subject = new Subject<int>();
        var received = new List<int>();
        var tcs = new TaskCompletionSource<bool>();
        using var token = SToken.Create();
        
        subject.Throttle(TimeSpan.FromMilliseconds(100)).Subscribe(x => {
            received.Add(x);
            tcs.TrySetResult(true);
        }, token);
        
        subject.OnNext(1);
        await Task.Delay(50);
        subject.OnNext(2);
        
        await Task.WhenAny(tcs.Task, Task.Delay(500));
        
        Assert.That(received.Count, Is.EqualTo(1));
        Assert.That(received[0], Is.EqualTo(2));
    }

    // Hub Integration Tests
    [Test]
    public async Task Observable_FromHub_ReceivesHubMessages()
    {
        var hub = new Hub();
        var received = new List<TestMessage>();
        var tcs = new TaskCompletionSource<bool>();
        using var token = SToken.Create();
        
        var observable = hub.ToObservable<TestMessage>();
        observable.Subscribe(msg => {
            received.Add(msg);
            if (received.Count == 2)
                tcs.TrySetResult(true);
        }, token);
        
        var msg1 = new TestMessage { Value = 1 };
        var msg2 = new TestMessage { Value = 2 };
        
        hub.Publish(msg1);
        hub.Publish(msg2);
        
        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        
        Assert.That(received.Count, Is.EqualTo(2));
        Assert.That(received[0].Value, Is.EqualTo(1));
        Assert.That(received[1].Value, Is.EqualTo(2));
    }

    [Test]
    public void Observable_FromHub_Dispose_UnsubscribesFromHub()
    {
        var hub = new Hub();
        var received = new List<TestMessage>();
        
        var observable = hub.ToObservable<TestMessage>();
        var observer = Observer.Create<TestMessage>(received.Add);
        var subscription = observable.Subscribe(observer);
        
        hub.Publish(new TestMessage { Value = 1 });
        Thread.Sleep(100);
        
        subscription.Dispose();
        
        // After disposal, should not receive
        hub.Publish(new TestMessage { Value = 2 });
        Thread.Sleep(100);
        
        Assert.That(received.Count, Is.EqualTo(1));
    }

    // Async Subscribe Tests
    [Test]
    public async Task Observable_SubscribeAsync_ExecutesAsyncCallback()
    {
        var subject = new Subject<int>();
        var syncLock = new object();
        var received = new List<int>();
        var tcs = new TaskCompletionSource<bool>();
        using var token = SToken.Create();
        
        subject.Subscribe(async x => {
            await Task.Delay(50);
            lock (syncLock)
            {
                received.Add(x);
                if (received.Count == 3)
                    tcs.TrySetResult(true);
            }
        }, token);
        
        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);
        
        await Task.WhenAny(tcs.Task, Task.Delay(2000));
        
        // Async callbacks may complete out of order
        lock (syncLock)
        {
            Assert.That(received.Count, Is.EqualTo(3));
            Assert.That(received, Does.Contain(1));
            Assert.That(received, Does.Contain(2));
            Assert.That(received, Does.Contain(3));
        }
    }

    [Test]
    public async Task Observable_SubscribeAsync_WithOnComplete_CallsCallback()
    {
        var subject = new Subject<int>();
        var completed = false;
        var tcs = new TaskCompletionSource<bool>();
        using var token = SToken.Create();
        
        subject.Subscribe(async x => {
            await Task.Delay(10);
        }, () => {
            completed = true;
            tcs.TrySetResult(true);
        }, token);
        
        subject.OnNext(1);
        subject.OnCompleted();
        
        await Task.WhenAny(tcs.Task, Task.Delay(500));
        
        Assert.That(completed, Is.True);
    }

    // EventArgs overload test
    [Test]
    public void Observable_SubscribeEventArgs_CallsActionWithoutArgs()
    {
        var subject = new Subject<EventArgs>();
        var callCount = 0;
        using var token = SToken.Create();
        
        subject.Subscribe(() => callCount++, token);
        
        subject.OnNext(EventArgs.Empty);
        subject.OnNext(EventArgs.Empty);
        
        Assert.That(callCount, Is.EqualTo(2));
    }

    // IObserver subscription with CancellationToken
    [Test]
    public void Observable_SubscribeIObserver_WithCancellationToken_Unsubscribes()
    {
        var subject = new Subject<int>();
        var received = new List<int>();
        var observer = Observer.Create<int>(received.Add);
        using var cts = new CancellationTokenSource();
        
        subject.Subscribe(observer, cts.Token);
        
        subject.OnNext(1);
        cts.Cancel();
        subject.OnNext(2);
        
        Thread.Sleep(50); // Give time for cancellation
        Assert.That(received.Count, Is.EqualTo(1));
    }

    // FromEvent error handling
    [Test]
    public void Observable_FromEvent_InvalidEventName_ThrowsArgumentException()
    {
        var target = new TestEvent();
        
        Assert.Throws<ArgumentException>(() => 
            Observable.FromEvent<EventArgs>(target, "NonExistentEvent"));
    }

    [Test]
    public void Observable_FromEvent_WithPropertyChanged_Works()
    {
        var target = new TestEvent();
        var received = new List<PropertyChangedEventArgs>();
        using var token = SToken.Create();
        
        var observable = Observable.FromEvent<PropertyChangedEventArgs>(target, nameof(target.PropertyChanged));
        observable.Subscribe(received.Add, token);
        
        target.RaisePropertyChanged("TestProperty");
        target.RaisePropertyChanged("AnotherProperty");
        
        Assert.That(received.Count, Is.EqualTo(2));
        Assert.That(received[0].PropertyName, Is.EqualTo("TestProperty"));
        Assert.That(received[1].PropertyName, Is.EqualTo("AnotherProperty"));
    }

    [Test]
    public void Observable_FromEvent_Dispose_RemovesEventHandler()
    {
        var target = new TestEvent();
        var received = new List<EventArgs>();
        
        var observable = Observable.FromEvent<EventArgs>(target, nameof(target.Event));
        var observer = Observer.Create<EventArgs>(received.Add);
        var subscription = observable.Subscribe(observer);
        
        target.RaiseEvent();
        subscription.Dispose();
        
        // After disposal, should not receive
        target.RaiseEvent();
        
        Assert.That(received.Count, Is.EqualTo(1));
    }
}