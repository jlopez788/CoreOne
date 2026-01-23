using CoreOne.Results;
using NUnit.Framework;

namespace Tests.Results;

public class ResultTests
{
    [Test]
    public void Result_Ok_IsSuccess()
    {
        var result = Result.Ok;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Success));
        }
    }

    [Test]
    public void Result_Fail_IsNotSuccess()
    {
        var result = Result.Fail();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Fail));
        }
    }

    [Test]
    public void Result_FailWithMessage_ContainsMessage()
    {
        var result = Result.Fail("Error occurred");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Error occurred"));
        }
    }

    [Test]
    public void Result_FromException_IsException()
    {
        var exception = new InvalidOperationException("Test error");
        var result = Result.FromException(exception);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Exception));
            Assert.That(result.Message, Does.Contain("Test error"));
        }
    }

    [Test]
    public void ResultT_WithValue_IsSuccess()
    {
        var result = new Result<int>(42);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo(42));
        }
    }

    [Test]
    public void ResultT_WithNullValue_IsSuccess()
    {
        var result = new Result<string?>(null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.Null);
        }
    }

    [Test]
    public void ResultT_Fail_IsNotSuccess()
    {
        var result = Result.Fail<int>("Error");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Error"));
        }
    }

    [Test]
    public void ResultT_FromException_IsException()
    {
        var exception = new InvalidOperationException("Test error");
        var result = Result.FromException<int>(exception);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Exception));
        }
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ResultType.Success, Is.Not.EqualTo(ResultType.Exception));
            Assert.That(ResultType.Fail, Is.Not.EqualTo(ResultType.Exception));
        }
    }



    [Test]
    public void ResultT_PreservesGenericType()
    {
        var stringResult = new Result<string>("test");
        var intResult = new Result<int>(42);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(stringResult.Model, Is.TypeOf<string>());
            Assert.That(intResult.Model, Is.TypeOf<int>());
        }
    }

    [Test]
    public void Result_DefaultConstructor_IsSuccess()
    {
        var result = new Result();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Success));
            Assert.That(result.Success, Is.True);
        }
    }

    [Test]
    public void ResultT_DefaultConstructor_IsSuccess()
    {
        var result = new Result<int>();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Success));
            Assert.That(result.Success, Is.True);
        }
    }

    [Test]
    public void ResultT_WithModel_AndSuccessResultType()
    {
        var result = new Result<string>("test", ResultType.Success);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo("test"));
        }
    }

    [Test]
    public void ResultT_WithRequireInstance_NullModel_Fails()
    {
        var result = new Result<string>(null, requireInstance: true);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("null"));
        }
    }

    [Test]
    public void ResultT_WithRequireInstance_NonNullModel_Succeeds()
    {
        var result = new Result<string>("value", requireInstance: true);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo("value"));
        }
    }

    [Test]
    public void Result_FromException_TaskCanceledException_ReturnsFail()
    {
        var exception = new TaskCanceledException();
        var result = Result.FromException(exception);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Fail));
            Assert.That(result.Message, Does.Contain("cancel"));
        }
    }

    [Test]
    public void Result_FromException_OperationCanceledException_ReturnsFail()
    {
        var exception = new OperationCanceledException();
        var result = Result.FromException(exception);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Fail));
            Assert.That(result.Message, Does.Contain("cancel"));
        }
    }

    [Test]
    public void Result_FromException_ObjectDisposedException_ReturnsFail()
    {
        var exception = new ObjectDisposedException("TestObject");
        var result = Result.FromException(exception);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Fail));
            Assert.That(result.Message, Does.Contain("disposed"));
        }
    }

    [Test]
    public void Result_FromException_NullReferenceException_ReturnsException()
    {
        var exception = new NullReferenceException();
        var result = Result.FromException(exception);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Exception));
            Assert.That(result.Message, Does.Contain("Null reference"));
        }
    }

    [Test]
    public void ResultT_FromException_PreservesExceptionType()
    {
        var exception = new InvalidOperationException("Test");
        var result = Result.FromException<int>(exception);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Exception));
            Assert.That(result.Model, Is.EqualTo(0));
        }
    }

    [Test]
    public void Result_FailWithDefaultMessage_HasValidMessage()
    {
        var result = Result.Fail();
        
        Assert.That(result.Message, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void ResultT_FailWithDefaultMessage_HasValidMessage()
    {
        var result = Result.Fail<int>();
        
        Assert.That(result.Message, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void ResultT_WithResultType_SetsResultTypeCorrectly()
    {
        var failResult = new Result<int>(ResultType.Fail, "Failed");
        var exceptionResult = new Result<int>(ResultType.Exception, "Error");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(failResult.ResultType, Is.EqualTo(ResultType.Fail));
            Assert.That(exceptionResult.ResultType, Is.EqualTo(ResultType.Exception));
        }
    }

    [Test]
    public void Result_Ok_IsSingleton()
    {
        var result1 = Result.Ok;
        var result2 = Result.Ok;
        
        Assert.That(result1, Is.SameAs(result2));
    }

    [Test]
    public void Result_Success_ImpliesSuccessTrue()
    {
        var result = new Result(ResultType.Success, "");
        
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void Result_NonSuccess_ImpliesSuccessFalse()
    {
        var failResult = new Result(ResultType.Fail, "");
        var exceptionResult = new Result(ResultType.Exception, "");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(failResult.Success, Is.False);
            Assert.That(exceptionResult.Success, Is.False);
        }
    }
}
