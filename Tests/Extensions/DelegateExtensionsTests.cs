using CoreOne.Extensions;

namespace Tests.Extensions;

public class DelegateExtensionsTests
{
    [Test]
    public async Task AsTask_Action_ConvertsToTask()
    {
        var executed = false;
        Action action = () => executed = true;
        
        var taskFunc = action.AsTask();
        await taskFunc();
        
        Assert.That(executed, Is.True);
    }

    [Test]
    public async Task AsTask_NullAction_ReturnsCompletedTask()
    {
        Action? action = null;
        
        var taskFunc = action.AsTask();
        await taskFunc();
        
        Assert.Pass("Null action handled without exception");
    }

    [Test]
    public async Task AsTask_GenericAction_ConvertsToTask()
    {
        var result = 0;
        Action<int> action = x => result = x;
        
        var taskFunc = action.AsTask();
        await taskFunc(42);
        
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task AsTask_NullGenericAction_ReturnsCompletedTask()
    {
        Action<int>? action = null;
        
        var taskFunc = action.AsTask();
        await taskFunc(42);
        
        Assert.Pass("Null action handled without exception");
    }

    [Test]
    public async Task AsTask_ActionWithString_WorksCorrectly()
    {
        string? result = null;
        Action<string> action = s => result = s;
        
        var taskFunc = action.AsTask();
        await taskFunc("test");
        
        Assert.That(result, Is.EqualTo("test"));
    }

    [Test]
    public async Task AsTask_MultipleInvocations_WorksCorrectly()
    {
        var counter = 0;
        Action action = () => counter++;
        
        var taskFunc = action.AsTask();
        await taskFunc();
        await taskFunc();
        await taskFunc();
        
        Assert.That(counter, Is.EqualTo(3));
    }
}
