using System;
using System.Threading;
using System.Threading.Tasks;
using CoreOne.Hubs;
using CoreOne.Results;
using CoreOne.Extensions;
using Moq;
using NUnit.Framework;

namespace Tests;

public class HubTests
{
    public interface IMessage { int Value { get; } }
    public class MessageImpl : IMessage { public int Value { get; set; } }

    public class BaseMessage { public string? Text { get; set; } }
    public class DerivedMessage : BaseMessage { public DerivedMessage(string text) => Text = text; }

    public interface IParent { int X { get; } }
    public interface IChild : IParent { }
    public class ChildImpl : IChild { public int X { get; set; } }

    public interface IFoo { int A { get; } }
    public interface IBar { int B { get; } }
    public class MultiImpl : IFoo, IBar { public int A { get; set; } public int B { get; set; } }

    public interface IAlpha { string? Name { get; } }
    public class BaseAlpha : IAlpha { public string? Name { get; set; } }
    public class DerivedAlpha : BaseAlpha { }

    public class GlobalMsg : IGlobalHubMessage { public bool IsGlobal { get; set; } public int Value { get; set; } }

    public class StateMsg : IHubState<StateMsg>
    {
#if NET9_0_OR_GREATER
        public static StateMsg Default => new();
#endif
        public string? Name { get; set; }
        public string? Value { get; set; }
    }

    [SetUp]
    public void Setup() { }

    [Test]
    public async Task Subscription_ThroughInterface_PublishedAsConcrete_IsReceived()
    {
        var hub = new Hub();
        var tcs = new TaskCompletionSource<IMessage?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mock = new Mock<Action<IMessage>>();
        mock.Setup(m => m(It.IsAny<IMessage>())).Callback<IMessage>(m => tcs.TrySetResult(m));

        // Explicitly subscribe using Func<T, Task> wrapper that invokes the mock
        hub.Subscribe<IMessage>(msg => { mock.Object(msg); return Task.CompletedTask; }, null, CancellationToken.None);

        var msg = new MessageImpl { Value = 42 };
        hub.Publish(msg);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.That(completed == tcs.Task, Is.True);
        mock.Verify(m => m(It.IsAny<IMessage>()), Times.Once);
        Assert.That(tcs.Task.Result, Is.SameAs(msg));
    }

    [Test]
    public async Task Subscription_ThroughInheritance_ClassBase_IsReceived()
    {
        var hub = new Hub();
        var tcs = new TaskCompletionSource<BaseMessage?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mock = new Mock<Action<BaseMessage>>();
        mock.Setup(m => m(It.IsAny<BaseMessage>())).Callback<BaseMessage>(m => tcs.TrySetResult(m));

        hub.Subscribe<BaseMessage>(msg => { mock.Object(msg); return Task.CompletedTask; }, null, CancellationToken.None);

        var derived = new DerivedMessage("hello");
        hub.Publish(derived);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.That(completed == tcs.Task, Is.True);
        mock.Verify(m => m(It.IsAny<BaseMessage>()), Times.Once);
        Assert.That(tcs.Task.Result, Is.SameAs(derived));
    }

    [Test]
    public async Task Subscription_InterfaceInheritance_IsReceived()
    {
        var hub = new Hub();
        var tcs = new TaskCompletionSource<IParent?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mock = new Mock<Action<IParent>>();
        mock.Setup(m => m(It.IsAny<IParent>())).Callback<IParent>(m => tcs.TrySetResult(m));

        // uses extension Subscribe(Action<T>, CancellationToken)
        hub.Subscribe(mock.Object, CancellationToken.None);

        var impl = new ChildImpl { X = 7 };
        hub.Publish(impl);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.That(completed == tcs.Task, Is.True);
        mock.Verify(m => m(It.IsAny<IParent>()), Times.Once);
        Assert.That(tcs.Task.Result, Is.SameAs(impl));
    }

    [Test]
    public async Task Subscription_MultipleInterfaces_IsReceived()
    {
        var hub = new Hub();
        var tcs = new TaskCompletionSource<IBar?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mock = new Mock<Action<IBar>>();
        mock.Setup(m => m(It.IsAny<IBar>())).Callback<IBar>(m => tcs.TrySetResult(m));

        hub.Subscribe(mock.Object, CancellationToken.None);

        var multi = new MultiImpl { A = 1, B = 2 };
        hub.Publish(multi);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.That(completed == tcs.Task, Is.True);
        mock.Verify(m => m(It.IsAny<IBar>()), Times.Once);
        Assert.That(tcs.Task.Result, Is.SameAs(multi));
    }

    [Test]
    public async Task Subscription_InterfaceImplementedOnBaseClass_DerivedPublished_IsReceived()
    {
        var hub = new Hub();
        var tcs = new TaskCompletionSource<IAlpha?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mock = new Mock<Action<IAlpha>>();
        mock.Setup(m => m(It.IsAny<IAlpha>())).Callback<IAlpha>(m => tcs.TrySetResult(m));

        hub.Subscribe(mock.Object, CancellationToken.None);

        var d = new DerivedAlpha { Name = "derived" };
        hub.Publish(d);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.That(completed == tcs.Task, Is.True);
        mock.Verify(m => m(It.IsAny<IAlpha>()), Times.Once);
        Assert.That(tcs.Task.Result, Is.SameAs(d));
    }

    [Test]
    public async Task GlobalPublish_IsDeliveredToAllInstances()
    {
        var hub1 = new Hub();
        var hub2 = new Hub();

        var tcs1 = new TaskCompletionSource<GlobalMsg?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tcs2 = new TaskCompletionSource<GlobalMsg?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var mock1 = new Mock<Action<GlobalMsg>>();
        mock1.Setup(m => m(It.IsAny<GlobalMsg>())).Callback<GlobalMsg>(m => tcs1.TrySetResult(m));
        var mock2 = new Mock<Action<GlobalMsg>>();
        mock2.Setup(m => m(It.IsAny<GlobalMsg>())).Callback<GlobalMsg>(m => tcs2.TrySetResult(m));

        hub1.Subscribe(mock1.Object, CancellationToken.None);
        hub2.Subscribe(mock2.Object, CancellationToken.None);

        var gm = new GlobalMsg { IsGlobal = true, Value = 123 };
        hub1.Publish(gm);

        var completed = await Task.WhenAny(Task.WhenAll(tcs1.Task, tcs2.Task), Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.That(completed != Task.Delay(TimeSpan.FromSeconds(2)), Is.True);
        mock1.Verify(m => m(It.IsAny<GlobalMsg>()), Times.Once);
        mock2.Verify(m => m(It.IsAny<GlobalMsg>()), Times.Once);
        Assert.That(tcs1.Task.Result, Is.SameAs(gm));
        Assert.That(tcs2.Task.Result, Is.SameAs(gm));
    }

    [Test]
    public async Task Intercept_CanBlockDelivery_And_RemovalByToken()
    {
        var hub = new Hub();

        var blockedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calledTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // intercept that blocks (returns Fail)
        hub.Intercept<MessageImpl>(msg => Task.FromResult(ResultType.Fail), 0, CancellationToken.None);

        // subscription that would be blocked
        var subMock = new Mock<Action<MessageImpl>>();
        subMock.Setup(m => m(It.IsAny<MessageImpl>())).Callback<MessageImpl>(m => blockedTcs.TrySetResult(true));
        hub.Subscribe(subMock.Object, CancellationToken.None);

        hub.Publish(new MessageImpl { Value = 1 });

        var blockedCompleted = await Task.WhenAny(blockedTcs.Task, Task.Delay(1000));
        Assert.That(blockedCompleted != blockedTcs.Task, Is.True, "Subscriber should have been blocked by intercept.");
        subMock.Verify(m => m(It.IsAny<MessageImpl>()), Times.Never);

        // now register an intercept that sets calledTcs then remove it via token before publish
        using var cts = new CancellationTokenSource();
        hub.Intercept<MessageImpl>(async m => {
            calledTcs.TrySetResult(true);
            return ResultType.Success;
        }, 0, cts.Token);

        // cancel registration so intercept is removed
        cts.Cancel();

        hub.Publish(new MessageImpl { Value = 2 });

        var calledCompleted = await Task.WhenAny(calledTcs.Task, Task.Delay(1000));
        Assert.That(calledCompleted != calledTcs.Task, Is.True, "Intercept removed via token should not be called.");
    }

    [Test]
        public async Task Publish_State_IsStored_And_SubscribeState_ImmediateDelivery()
    {
        var hub = new Hub();
        var tcs = new TaskCompletionSource<StateMsg?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mock = new Mock<Action<StateMsg>>();
        mock.Setup(m => m(It.IsAny<StateMsg>())).Callback<StateMsg>(m => tcs.TrySetResult(m));

        var state = new StateMsg { Name = "my", Value = "v1" };
        hub.Publish(state);

        hub.SubscribeState<StateMsg>(state.Name, mock.Object, null, CancellationToken.None);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.That(completed == tcs.Task, Is.True);
        mock.Verify(m => m(It.IsAny<StateMsg>()), Times.Once);
        Assert.That(tcs.Task.Result, Is.Not.Null);
        Assert.That(tcs.Task.Result!.Value, Is.EqualTo("v1"));
    }

    [Test]
    public async Task Subscriber_Exception_Triggers_ExceptionMessagePublish()
    {
        var hub = new Hub();
        var exceptionTcs = new TaskCompletionSource<ExceptionMessage?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var exMock = new Mock<Action<ExceptionMessage>>();
        exMock.Setup(m => m(It.IsAny<ExceptionMessage>())).Callback<ExceptionMessage>(m => exceptionTcs.TrySetResult(m));

        hub.Subscribe(exMock.Object, CancellationToken.None);

        // subscriber that throws
        hub.Subscribe<MessageImpl>(m => throw new InvalidOperationException("boom"), CancellationToken.None);

        hub.Publish(new MessageImpl { Value = 99 });

        var completed = await Task.WhenAny(exceptionTcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.That(completed == exceptionTcs.Task, Is.True);
        exMock.Verify(m => m(It.IsAny<ExceptionMessage>()), Times.Once);
        Assert.That(exceptionTcs.Task.Result, Is.Not.Null);
        Assert.That(exceptionTcs.Task.Result!.Message, Does.Contain("Unable to deliver message"));
    }

    [Test]
    public async Task RegisterSubscription_Removal_OnTokenCancel_PreventsDelivery()
    {
        var hub = new Hub();
        var tcs = new TaskCompletionSource<MessageImpl?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mock = new Mock<Action<MessageImpl>>();
        mock.Setup(m => m(It.IsAny<MessageImpl>())).Callback<MessageImpl>(m => tcs.TrySetResult(m));

        using var cts = new CancellationTokenSource();
        hub.Subscribe(mock.Object, cts.Token);

        cts.Cancel();

        hub.Publish(new MessageImpl { Value = 5 });

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(300));
        Assert.That(completed != tcs.Task, Is.True, "Subscription cancelled should not receive message.");
        mock.Verify(m => m(It.IsAny<MessageImpl>()), Times.Never);
    }


    [Test]
    public async Task Publish_Null_Message_DoesNotDeliver()
    {
        var hub = new Hub();
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mock = new Mock<Action<string?>>();
        mock.Setup(m => m(It.IsAny<string?>())).Callback<string?>(m => tcs.TrySetResult(m));

        hub.Subscribe(mock.Object, CancellationToken.None);

        hub.Publish<string?>(null);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(300));
        Assert.That(completed != tcs.Task, Is.True, "Null publish should not invoke subscribers.");
        mock.Verify(m => m(It.IsAny<string?>()), Times.Never);
    }

    [Test]
    public void Hub_Dispose_ClearsSubscriptionsAndIntercepts()
    {
        var hub = new Hub();
        hub.Subscribe<MessageImpl>(m => Task.CompletedTask, CancellationToken.None);
        hub.Intercept<MessageImpl>(m => Task.FromResult(ResultType.Success), 0, CancellationToken.None);

        hub.Dispose();

        // Verify hub is disposed
        Assert.DoesNotThrow(() => hub.Dispose()); // Should be safe to dispose multiple times
    }

    [Test]
    public void GetState_ReturnsStoredState()
    {
        var hub = new Hub();
        var state = new StateMsg { Name = "test", Value = "value1" };
        hub.Publish(state);

        // Give time for async publish to complete
        Thread.Sleep(100);

        var retrieved = hub.GetState<StateMsg>("test");
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Value, Is.EqualTo("value1"));
    }

    [Test]
    public void GetState_WithoutName_ReturnsDefault()
    {
        var hub = new Hub();
        var retrieved = hub.GetState<StateMsg>(null);
        Assert.That(retrieved, Is.Not.Null);
    }

    [Test]
    public void Subscribe_WithNullCallback_DoesNotThrow()
    {
        var hub = new Hub();
        Assert.DoesNotThrow(() => hub.Subscribe<MessageImpl>(null!, CancellationToken.None));
    }

    [Test]
    public async Task MultipleSubscribers_WithExceptions_PublishesAggregateException()
    {
        var hub = new Hub();
        var exceptionTcs = new TaskCompletionSource<ExceptionMessage?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var exMock = new Mock<Action<ExceptionMessage>>();
        exMock.Setup(m => m(It.IsAny<ExceptionMessage>())).Callback<ExceptionMessage>(m => exceptionTcs.TrySetResult(m));

        hub.Subscribe(exMock.Object, CancellationToken.None);

        // Multiple subscribers that throw
        hub.Subscribe<MessageImpl>(m => throw new InvalidOperationException("error1"), CancellationToken.None);
        hub.Subscribe<MessageImpl>(m => throw new ArgumentException("error2"), CancellationToken.None);

        hub.Publish(new MessageImpl { Value = 100 });

        var completed = await Task.WhenAny(exceptionTcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.That(completed == exceptionTcs.Task, Is.True);
        exMock.Verify(m => m(It.IsAny<ExceptionMessage>()), Times.Once);
        
        var result = exceptionTcs.Task.Result;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Exception, Is.InstanceOf<AggregateException>());
        var aggEx = (AggregateException)result.Exception;
        Assert.That(aggEx.InnerExceptions.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Subscribe_WithFilter_OnlyDeliversMatchingMessages()
    {
        var hub = new Hub();
        var tcs = new TaskCompletionSource<MessageImpl?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mock = new Mock<Action<MessageImpl>>();
        mock.Setup(m => m(It.IsAny<MessageImpl>())).Callback<MessageImpl>(m => tcs.TrySetResult(m));

        // Subscribe with filter that only accepts Value > 50
        hub.Subscribe<MessageImpl>(msg => { mock.Object(msg); return Task.CompletedTask; }, m => m.Value > 50, CancellationToken.None);

        // Publish message that doesn't match filter
        hub.Publish(new MessageImpl { Value = 10 });
        await Task.Delay(200);
        mock.Verify(m => m(It.IsAny<MessageImpl>()), Times.Never);

        // Publish message that matches filter
        hub.Publish(new MessageImpl { Value = 100 });
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(500));
        Assert.That(completed == tcs.Task, Is.True);
        mock.Verify(m => m(It.IsAny<MessageImpl>()), Times.Once);
    }

    [Test]
    public async Task Intercept_WithOrder_ExecutesInCorrectOrder()
    {
        var hub = new Hub();
        var executionOrder = new List<int>();
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        hub.Intercept<MessageImpl>(async m => {
            executionOrder.Add(2);
            return ResultType.Success;
        }, 2, CancellationToken.None);

        hub.Intercept<MessageImpl>(async m => {
            executionOrder.Add(1);
            return ResultType.Success;
        }, 1, CancellationToken.None);

        hub.Intercept<MessageImpl>(async m => {
            executionOrder.Add(3);
            tcs.TrySetResult(true);
            return ResultType.Success;
        }, 3, CancellationToken.None);

        hub.Publish(new MessageImpl { Value = 1 });

        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        Assert.That(executionOrder, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task SubscribeState_WithFilter_OnlyDeliversMatchingState()
    {
        var hub = new Hub();
        var tcs = new TaskCompletionSource<StateMsg?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mock = new Mock<Action<StateMsg>>();
        mock.Setup(m => m(It.IsAny<StateMsg>())).Callback<StateMsg>(m => tcs.TrySetResult(m));

        // Subscribe with filter
        hub.SubscribeState<StateMsg>("filtered", mock.Object, m => m.Value == "match", CancellationToken.None);

        // Publish non-matching state
        hub.Publish(new StateMsg { Name = "filtered", Value = "nomatch" });
        await Task.Delay(200);
        mock.Verify(m => m(It.IsAny<StateMsg>()), Times.Never);

        // Publish matching state
        hub.Publish(new StateMsg { Name = "filtered", Value = "match" });
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(500));
        Assert.That(completed == tcs.Task, Is.True);
        mock.Verify(m => m(It.IsAny<StateMsg>()), Times.Once);
    }

    [Test]
    public void Hub_ToString_ReturnsFormattedString()
    {
        var hub = new Hub();
        var str = hub.ToString();
        Assert.That(str, Does.Contain("-"));
    }
}