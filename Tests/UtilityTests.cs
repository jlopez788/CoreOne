using CoreOne;
using CoreOne.Results;
using NUnit.Framework;

namespace Tests;

public class UtilityTests
{
    [Test]
    public void FormatPhoneNumber_SevenDigits_FormatsCorrectly()
    {
        var result = Utility.FormatPhoneNumber("1234567");
        Assert.That(result, Is.EqualTo("123-4567"));
    }

    [Test]
    public void FormatPhoneNumber_TenDigits_FormatsCorrectly()
    {
        var result = Utility.FormatPhoneNumber("1234567890");
        Assert.That(result, Is.EqualTo("(123) 456-7890"));
    }

    [Test]
    public void FormatPhoneNumber_WithCountryCode_FormatsCorrectly()
    {
        var result = Utility.FormatPhoneNumber("11234567890");
        Assert.That(result, Is.EqualTo("+1 (123) 456-7890"));
    }

    [Test]
    public void FormatPhoneNumber_WithMask_MasksDigits()
    {
        var result = Utility.FormatPhoneNumber("1234567890", mask: true);
        Assert.That(result, Is.EqualTo("(***) ***-7890"));
    }

    [Test]
    public void FormatPhoneNumber_SevenDigitsWithMask_MasksCorrectly()
    {
        var result = Utility.FormatPhoneNumber("1234567", mask: true);
        Assert.That(result, Is.EqualTo("***-4567"));
    }

    [Test]
    public void FormatPhoneNumber_RemovesNonNumericCharacters()
    {
        var result = Utility.FormatPhoneNumber("(123) 456-7890");
        Assert.That(result, Is.EqualTo("(123) 456-7890"));
    }

    [Test]
    public void FormatPhoneNumber_NullInput_ReturnsEmpty()
    {
        var result = Utility.FormatPhoneNumber(null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FormatPhoneNumber_EmptyInput_ReturnsEmpty()
    {
        var result = Utility.FormatPhoneNumber("");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Try_Action_CatchesException()
    {
        var result = Utility.Try((Action)(() => throw new InvalidOperationException("Test error")));
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.ResultType, Is.EqualTo(ResultType.Exception));
        Assert.That(result.Message, Does.Contain("Test error"));
    }

    [Test]
    public void Try_Action_ReturnsSuccess_WhenNoException()
    {
        var executed = false;
        var result = Utility.Try(() => executed = true);
        
        Assert.That(result.Success, Is.True);
        Assert.That(executed, Is.True);
    }

    [Test]
    public async Task Try_AsyncAction_CatchesException()
    {
        var result = await Utility.Try(async () => {
            await Task.Delay(1);
            throw new InvalidOperationException("Async error");
        });
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.ResultType, Is.EqualTo(ResultType.Exception));
        Assert.That(result.Message, Does.Contain("Async error"));
    }

    [Test]
    public async Task Try_AsyncAction_ReturnsSuccess_WhenNoException()
    {
        var executed = false;
        var result = await Utility.Try(async () => {
            await Task.Delay(1);
            executed = true;
        });
        
        Assert.That(result.Success, Is.True);
        Assert.That(executed, Is.True);
    }

    [Test]
    public void Try_Func_ReturnsValue_WhenNoException()
    {
        var result = Utility.Try(() => 42);
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Model, Is.EqualTo(42));
    }

    [Test]
    public void Try_Func_CatchesException()
    {
        var result = Utility.Try((Func<int>)(() => throw new InvalidOperationException("Error")));
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.ResultType, Is.EqualTo(ResultType.Exception));
    }

    [Test]
    public async Task Try_AsyncFunc_ReturnsValue_WhenNoException()
    {
        var result = await Utility.Try(async () => {
            await Task.Delay(1);
            return 42;
        });
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Model, Is.EqualTo(42));
    }

    [Test]
    public async Task Try_AsyncFunc_CatchesException()
    {
        var result = await Utility.Try<int>(async () => {
            await Task.Delay(1);
            throw new InvalidOperationException("Async error");
        });
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.ResultType, Is.EqualTo(ResultType.Exception));
    }

    [Test]
    public void TryChangeType_ConvertsInt_ToDouble()
    {
        var success = Utility.TryChangeType<double>(42, out var result);
        
        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(42.0));
    }

    [Test]
    public void TryChangeType_ConvertsString_ToInt()
    {
        var success = Utility.TryChangeType<int>("123", out var result);
        
        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(123));
    }

    [Test]
    public void TryChangeType_ConvertsString_ToGuid()
    {
        var guid = Guid.NewGuid();
        var success = Utility.TryChangeType<Guid>(guid.ToString(), out var result);
        
        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(guid));
    }

    [Test]
    public void TryChangeType_ReturnsFalse_ForInvalidConversion()
    {
        var success = Utility.TryChangeType<int>("not a number", out var result);
        
        Assert.That(success, Is.False);
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void TryChangeType_HandlesNullableTypes()
    {
        var success = Utility.TryChangeType<int?>("42", out var result);
        
        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void EncodeForUrl_EncodesSpecialCharacters()
    {
        var result = Utility.EncodeForUrl("hello world");
        Assert.That(result, Is.EqualTo("hello+world"));
    }

    [Test]
    public void EncodeForUrl_EncodesSpecialSymbols()
    {
        var result = Utility.EncodeForUrl("test&value=123");
        Assert.That(result, Does.Contain("%26"));
    }

    [Test]
    public async Task SafeAwait_Task_AwaitsNonNullTask()
    {
        var executed = false;
        Task task = Task.Run(() => executed = true);
        
        await Utility.SafeAwait(task);
        
        Assert.That(executed, Is.True);
    }

    [Test]
    public async Task SafeAwait_Task_HandlesNullTask()
    {
        Task? task = null;
        
        await Utility.SafeAwait(task);
        
        // Should complete without throwing
        Assert.Pass();
    }

    [Test]
    public async Task SafeAwait_GenericTask_ReturnsValue()
    {
        Task<int> task = Task.FromResult(42);
        
        var result = await Utility.SafeAwait(task);
        
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task SafeAwait_GenericTask_ReturnsDefault_WhenNull()
    {
        Task<int>? task = null;
        
        var result = await Utility.SafeAwait(task);
        
        Assert.That(result, Is.EqualTo(0));
    }
}
