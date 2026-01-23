using CoreOne.Results;
using NUnit.Framework;

namespace Tests.Results;

public class ResultTests
{
    [Test]
    public void Result_Ok_IsSuccess()
    {
        var result = Result.Ok;
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.ResultType, Is.EqualTo(ResultType.Success));
    }

    [Test]
    public void Result_Fail_IsNotSuccess()
    {
        var result = Result.Fail();
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.ResultType, Is.EqualTo(ResultType.Fail));
    }

    [Test]
    public void Result_FailWithMessage_ContainsMessage()
    {
        var result = Result.Fail("Error occurred");
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.EqualTo("Error occurred"));
    }

    [Test]
    public void Result_FromException_IsException()
    {
        var exception = new InvalidOperationException("Test error");
        var result = Result.FromException(exception);
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.ResultType, Is.EqualTo(ResultType.Exception));
        Assert.That(result.Message, Does.Contain("Test error"));
    }

    [Test]
    public void ResultT_WithValue_IsSuccess()
    {
        var result = new Result<int>(42);
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Model, Is.EqualTo(42));
    }

    [Test]
    public void ResultT_WithNullValue_IsSuccess()
    {
        var result = new Result<string?>(null);
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Model, Is.Null);
    }

    [Test]
    public void ResultT_Fail_IsNotSuccess()
    {
        var result = Result.Fail<int>("Error");
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.EqualTo("Error"));
    }

    [Test]
    public void ResultT_FromException_IsException()
    {
        var exception = new InvalidOperationException("Test error");
        var result = Result.FromException<int>(exception);
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.ResultType, Is.EqualTo(ResultType.Exception));
    }

    [Test]
    public void ResultT_DefaultModel_ForFailedResult()
    {
        var result = Result.Fail<int>("Error");
        
        Assert.That(result.Model, Is.EqualTo(0));
    }

    [Test]
    public void ResultTypes_HasCorrectValues()
    {
        Assert.That(ResultType.Success, Is.Not.EqualTo(ResultType.Fail));
        Assert.That(ResultType.Success, Is.Not.EqualTo(ResultType.Exception));
        Assert.That(ResultType.Fail, Is.Not.EqualTo(ResultType.Exception));
    }



    [Test]
    public void ResultT_PreservesGenericType()
    {
        var stringResult = new Result<string>("test");
        var intResult = new Result<int>(42);
        
        Assert.That(stringResult.Model, Is.TypeOf<string>());
        Assert.That(intResult.Model, Is.TypeOf<int>());
    }
}
