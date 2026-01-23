using CoreOne.Extensions;
using CoreOne.Results;
using NUnit.Framework;

namespace Tests.Extensions;

public class ResultExtensionsTests
{
    [Test]
    public void Select_TransformsSuccessResult()
    {
        var result = new Result<int>(42);
        var transformed = result.Select(x => x.ToString());
        
        Assert.That(transformed.Success, Is.True);
        Assert.That(transformed.Model, Is.EqualTo("42"));
    }

    [Test]
    public void Select_PreservesFailureResult()
    {
        var result = Result.Fail<int>("Error occurred");
        var transformed = result.Select(x => x.ToString());
        
        Assert.That(transformed.Success, Is.False);
        Assert.That(transformed.Message, Is.EqualTo("Error occurred"));
    }

    [Test]
    public void Select_PreservesExceptionResult()
    {
        var exception = new InvalidOperationException("Test exception");
        var result = Result.FromException<int>(exception);
        var transformed = result.Select(x => x.ToString());
        
        Assert.That(transformed.Success, Is.False);
        Assert.That(transformed.ResultType, Is.EqualTo(ResultType.Exception));
    }

    [Test]
    public async Task SelectAsync_TransformsSuccessResult()
    {
        var result = new Result<int>(42);
        var transformed = await result.SelectAsync(async x => {
            await Task.Delay(1);
            return x.ToString();
        });
        
        Assert.That(transformed.Success, Is.True);
        Assert.That(transformed.Model, Is.EqualTo("42"));
    }

    [Test]
    public async Task SelectAsync_PreservesFailureResult()
    {
        var result = Result.Fail<int>("Error occurred");
        var callbackInvoked = false;
        var transformed = await result.SelectAsync(async x => {
            callbackInvoked = true;
            await Task.Delay(1);
            return x.ToString();
        });
        
        Assert.That(transformed.Success, Is.False);
        Assert.That(callbackInvoked, Is.False);
    }


}
