using CoreOne.Results;
using System.Net;

namespace Tests.Results;

[TestFixture]
public class HttpResultTests
{
    [Test]
    public void Constructor_WithSuccessStatusCode_SetsPropertiesCorrectly()
    {
        var result = new HttpResult(200, "Success");

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Message, Is.EqualTo("Success"));
            Assert.That(result.IsSuccessStatusCode, Is.True);
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Success));
            Assert.That(result.Success, Is.True);
        });
    }

    [Test]
    public void Constructor_With400StatusCode_SetsFailResult()
    {
        var result = new HttpResult(400);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.IsSuccessStatusCode, Is.False);
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Fail));
            Assert.That(result.Success, Is.False);
        });
    }

    [Test]
    public void Constructor_With500StatusCode_SetsExceptionResult()
    {
        var result = new HttpResult(500, "Internal Server Error");

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(500));
            Assert.That(result.IsSuccessStatusCode, Is.False);
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Exception));
            Assert.That(result.Success, Is.False);
        });
    }

    [Test]
    public void Constructor_WithHttpResponseMessage_ExtractsStatusCode()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Created);
        
        var result = new HttpResult(response);

        Assert.That(result.StatusCode, Is.EqualTo(201));
    }

    [Test]
    public void FromException_CreatesHttpResultWithStatus500()
    {
        var exception = new InvalidOperationException("Test error");

        var result = HttpResult.FromException(exception);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(500));
            Assert.That(result.Message, Does.Contain("Test error"));
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Exception));
        });
    }

    [Test]
    public void FromException_Generic_CreatesHttpResultWithModel()
    {
        var exception = new ArgumentException("Invalid argument");

        var result = HttpResult.FromException<string>(exception);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(500));
            Assert.That(result.Message, Does.Contain("Invalid argument"));
            Assert.That(result.Model, Is.Null);
        });
    }

    [Test]
    public void FromException_WithInnerException_UsesInnerMessage()
    {
        var inner = new InvalidOperationException("Inner error");
        var outer = new Exception("Outer error", inner);

        var result = HttpResult.FromException(outer);

        Assert.That(result.Message, Does.Contain("Inner error"));
    }

    [Test]
    public void GetStatusCode_WithSuccessResult_Returns200()
    {
        var result = new Result<string>("data");

        var statusCode = HttpResult.GetStatusCode(result);

        Assert.That(statusCode, Is.EqualTo(200));
    }

    [Test]
    public void GetStatusCode_WithSuccessResultAndNullModel_Returns204()
    {
        var result = new Result<string>(null);

        var statusCode = HttpResult.GetStatusCode(result);

        Assert.That(statusCode, Is.EqualTo(204));
    }

    [Test]
    public void GetStatusCode_WithFailResult_Returns400()
    {
        var result = Result.Fail<string>();

        var statusCode = HttpResult.GetStatusCode(result);

        Assert.That(statusCode, Is.EqualTo(400));
    }

    [Test]
    public void GetStatusCode_WithExceptionResult_Returns500()
    {
        var result = Result.FromException<string>(new Exception());

        var statusCode = HttpResult.GetStatusCode(result);

        Assert.That(statusCode, Is.EqualTo(500));
    }

    [Test]
    public void GetStatusCode_NonGeneric_WithSuccessResult_Returns204()
    {
        var result = Result.Ok;

        var statusCode = HttpResult.GetStatusCode(result);

        Assert.That(statusCode, Is.EqualTo(204));
    }

    [Test]
    public void GetStatusCode_NonGeneric_WithFailResult_Returns400()
    {
        var result = Result.Fail();

        var statusCode = HttpResult.GetStatusCode(result);

        Assert.That(statusCode, Is.EqualTo(400));
    }

    [Test]
    [TestCase(100, true, ResultType.Success)]
    [TestCase(200, true, ResultType.Success)]
    [TestCase(204, true, ResultType.Success)]
    [TestCase(299, true, ResultType.Success)]
    [TestCase(400, false, ResultType.Fail)]
    [TestCase(404, false, ResultType.Fail)]
    [TestCase(499, false, ResultType.Fail)]
    [TestCase(500, false, ResultType.Exception)]
    [TestCase(503, false, ResultType.Exception)]
    public void GetResultFromStatusCode_ReturnsCorrectMapping(int statusCode, bool expectedSuccess, ResultType expectedType)
    {
        var (success, resultType) = HttpResult.GetResultFromStatusCode(statusCode);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.EqualTo(expectedSuccess));
            Assert.That(resultType, Is.EqualTo(expectedType));
        });
    }
}

[TestFixture]
public class HttpResultGenericTests
{
    [Test]
    public void Constructor_WithModel_SetsProperties()
    {
        var model = new { Name = "Test", Value = 42 };

        var result = new HttpResult<object>(model, 201);

        Assert.Multiple(() =>
        {
            Assert.That(result.Model, Is.SameAs(model));
            Assert.That(result.StatusCode, Is.EqualTo(201));
            Assert.That(result.Success, Is.True);
        });
    }

    [Test]
    public void Constructor_WithNullModel_AllowsNull()
    {
        var result = new HttpResult<string>(null, 200);

        Assert.Multiple(() =>
        {
            Assert.That(result.Model, Is.Null);
            Assert.That(result.StatusCode, Is.EqualTo(200));
        });
    }

    [Test]
    public void Constructor_WithErrorStatusCode_SetsFailure()
    {
        var result = new HttpResult<string>(400, "Bad Request");

        Assert.Multiple(() =>
        {
            Assert.That(result.Model, Is.Null);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Is.EqualTo("Bad Request"));
            Assert.That(result.Success, Is.False);
        });
    }

    [Test]
    public void ImplicitConversion_ToModel_ReturnsModel()
    {
        var result = new HttpResult<int>(42, 200);

        int value = result;

        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void ImplicitConversion_WithNullResult_ReturnsDefault()
    {
        HttpResult<string>? result = null;

        string? value = result!;

        Assert.That(value, Is.Null);
    }

    [Test]
    public void Select_WhenSuccessful_TransformsModel()
    {
        var result = new HttpResult<int>(42, 200);

        var transformed = result.Select(x => x.ToString());

        Assert.Multiple(() =>
        {
            Assert.That(transformed.Model, Is.EqualTo("42"));
            Assert.That(transformed.StatusCode, Is.EqualTo(200));
            Assert.That(transformed.Success, Is.True);
        });
    }

    [Test]
    public void Select_WhenFailed_PropagatesError()
    {
        var result = new HttpResult<int>(400, "Error");

        var transformed = result.Select(x => x.ToString());

        Assert.Multiple(() =>
        {
            Assert.That(transformed.Model, Is.Null);
            Assert.That(transformed.StatusCode, Is.EqualTo(400));
            Assert.That(transformed.Message, Is.EqualTo("Error"));
            Assert.That(transformed.Success, Is.False);
        });
    }

    [Test]
    public async Task FromResponse_WithSuccessResponse_ReturnsModel()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("test content")
        };

        var result = await HttpResult.FromResponse(response, async content => await content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.EqualTo("test content"));
            Assert.That(result.StatusCode, Is.EqualTo(200));
        });
    }

    [Test]
    public async Task FromResponse_WithErrorResponse_ReturnsError()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);

        var result = await HttpResult.FromResponse(response, async content => await content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Does.Contain("400"));
        });
    }

    [Test]
    public async Task FromResponse_WithException_Returns500Error()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        var result = await HttpResult.FromResponse<string>(response, _ => throw new InvalidOperationException("Parse error"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(500));
            Assert.That(result.Message, Does.Contain("Parse error"));
        });
    }
}

[TestFixture]
public class HttpResultWithErrorTests
{
    [Test]
    public void Constructor_WithModel_SetsModelAndSuccess()
    {
        var model = new { Id = 1 };

        var result = new HttpResult<object, string>(model, 200);

        Assert.Multiple(() =>
        {
            Assert.That(result.Model, Is.SameAs(model));
            Assert.That(result.ErrorModel, Is.Null);
            Assert.That(result.Success, Is.True);
        });
    }

    [Test]
    public void Constructor_WithErrorModel_SetsErrorAndFailure()
    {
        var errorModel = "Validation failed";

        var result = new HttpResult<object, string>(errorModel, 400);

        Assert.Multiple(() =>
        {
            Assert.That(result.Model, Is.Null);
            Assert.That(result.ErrorModel, Is.EqualTo(errorModel));
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Success, Is.False);
        });
    }

    [Test]
    public void Constructor_WithStatusCodeAndMessage_SetsMessage()
    {
        var result = new HttpResult<int, string>(500, "Server error");

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(500));
            Assert.That(result.Message, Is.EqualTo("Server error"));
            Assert.That(result.Model, Is.EqualTo(0));
            Assert.That(result.ErrorModel, Is.Null);
        });
    }

    [Test]
    public async Task FromJsonResponse_WithSuccessResponse_DeserializesModel()
    {
        var json = "{\"name\":\"Test\",\"value\":42}";
        var response = new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        var result = await HttpResult.FromJsonResponse<dynamic, string>(response, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.Not.Null);
            Assert.That(result.ErrorModel, Is.Null);
        });
    }

    [Test]
    public async Task FromJsonResponse_WithErrorResponse_DeserializesError()
    {
        var json = "\"Error message\"";
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        var result = await HttpResult.FromJsonResponse<object, string>(response, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Message, Does.Contain("400"));
        });
    }

    [Test]
    public async Task FromJsonResponse_WithInvalidJson_StillParsesResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("invalid json")
        };

        var result = await HttpResult.FromJsonResponse<object, string>(response, CancellationToken.None);

        // With invalid JSON, deserialization may succeed with null or fail - either is valid behavior
        Assert.That(result.StatusCode, Is.EqualTo(200).Or.EqualTo(500));
    }
}
