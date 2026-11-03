using CoreOne;
using CoreOne.Extensions;
using CoreOne.Results;
using CoreOne.Services;
using System.Net;
using System.Text;

namespace Tests.Extensions;

public class HttpResultExtensionsTests
{
    public class CustomerModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class DuplicateMatch
    {
        public int ExistingId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ValidationError
    {
        public string Field { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, object payload) =>
        new(statusCode) {
            Content = new StringContent(Utility.Serialize(payload), Encoding.UTF8, "application/json")
        };

    [Test]
    public async Task GetContent_OnConflictResponse_DeserializesErrorModel()
    {
        using var response = CreateJsonResponse(HttpStatusCode.Conflict, new DuplicateMatch {
            ExistingId = 42,
            Reason = "Email already exists"
        });

        var httpResult = await HttpResult.FromResponseAsync<CustomerModel>(response, NJsonService.Instance);
        var contentResult = httpResult.GetContent<DuplicateMatch>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(httpResult.Success, Is.False);
            Assert.That(httpResult.StatusCode, Is.EqualTo(WebCodes.Status409Conflict));
            Assert.That(contentResult.Success, Is.True);
            Assert.That(contentResult.Model?.ExistingId, Is.EqualTo(42));
            Assert.That(contentResult.Model?.Reason, Is.EqualTo("Email already exists"));
        }
    }

    [Test]
    public async Task GetContent_StringAndByteArrayTargets_ReturnsRawErrorPayload()
    {
        var rawText = "Conflict details";
        using var response = new HttpResponseMessage(HttpStatusCode.Conflict) {
            Content = new StringContent(rawText, Encoding.UTF8, "text/plain")
        };

        var httpResult = await HttpResult.FromResponseAsync<CustomerModel>(response, NJsonService.Instance);
        var stringContent = httpResult.GetContent<string>();
        var bytesContent = httpResult.GetContent<byte[]>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stringContent.Success, Is.True);
            Assert.That(stringContent.Model, Is.EqualTo(rawText));
            Assert.That(bytesContent.Success, Is.True);
            Assert.That(bytesContent.Model, Is.EqualTo(Encoding.UTF8.GetBytes(rawText)));
        }
    }

    [Test]
    public async Task GetContent_EmptyErrorContent_ReturnsFailureResult()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Conflict) {
            Content = new StringContent(string.Empty)
        };

        var httpResult = await HttpResult.FromResponseAsync<CustomerModel>(response, NJsonService.Instance);
        var contentResult = httpResult.GetContent<DuplicateMatch>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contentResult.Success, Is.False);
            Assert.That(contentResult.Model, Is.Null);
        }
    }

    [Test]
    public void GetContent_FromMessageFallback_DeserializesWhenErrorContentNotSet()
    {
        var json = Utility.Serialize(new DuplicateMatch { ExistingId = 99, Reason = "Duplicate key" });
        var httpResult = new HttpResult<CustomerModel>(WebCodes.Status409Conflict, json);

        var contentResult = httpResult.GetContent<DuplicateMatch>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contentResult.Success, Is.True);
            Assert.That(contentResult.Model?.ExistingId, Is.EqualTo(99));
            Assert.That(contentResult.Model?.Reason, Is.EqualTo("Duplicate key"));
        }
    }

    [Test]
    public void GetContent_OnDualTypedHttpResult_ReturnsPreDeserializedErrorModel()
    {
        var duplicate = new DuplicateMatch { ExistingId = 15, Reason = "Already registered" };
        var httpResult = new HttpResult<CustomerModel, DuplicateMatch>(duplicate, WebCodes.Status409Conflict);

        var contentResult = httpResult.GetContent<DuplicateMatch>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contentResult.Success, Is.True);
            Assert.That(contentResult.Model, Is.SameAs(duplicate));
        }
    }

    [Test]
    public async Task OnFailAsync_WithMatchingStatusCode_DeserializesErrorAndChainsHandlers()
    {
        using var response = CreateJsonResponse(HttpStatusCode.Conflict, new DuplicateMatch {
            ExistingId = 77,
            Reason = "Duplicate customer"
        });

        DuplicateMatch? duplicate = null;
        ValidationError? validationError = null;
        CustomerModel? createdCustomer = null;

        var result = await HttpResult.FromResponseAsync<CustomerModel>(response, NJsonService.Instance)
            .OnSuccessAsync(c => createdCustomer = c)
            .OnFailAsync(WebCodes.Status400BadRequest, (ValidationError? err) => validationError = err)
            .OnFailAsync(WebCodes.Status409Conflict, (DuplicateMatch? dup) => duplicate = dup);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(WebCodes.Status409Conflict));
            Assert.That(createdCustomer, Is.Null);
            Assert.That(validationError, Is.Null);
            Assert.That(duplicate, Is.Not.Null);
            Assert.That(duplicate?.ExistingId, Is.EqualTo(77));
            Assert.That(duplicate?.Reason, Is.EqualTo("Duplicate customer"));
        }
    }

    [Test]
    public async Task OnFailAsync_WithAsyncCallbackAndStatusPredicate_InvokesWhenMatched()
    {
        using var response = CreateJsonResponse(HttpStatusCode.UnprocessableEntity, new ValidationError {
            Field = "Email",
            Error = "Invalid format"
        });

        ValidationError? captured = null;

        var result = await HttpResult.FromResponseAsync<CustomerModel>(response, NJsonService.Instance)
            .OnFailAsync(code => code is >= 400 and < 500, async (ValidationError? err) => {
                await Task.Delay(1);
                captured = err;
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.StatusCode, Is.EqualTo(WebCodes.Status422UnprocessableEntity));
            Assert.That(captured?.Field, Is.EqualTo("Email"));
            Assert.That(captured?.Error, Is.EqualTo("Invalid format"));
        }
    }

    [Test]
    public async Task OnFailAsync_NonGenericHttpResult_DeserializesTypedError()
    {
        var json = Utility.Serialize(new DuplicateMatch { ExistingId = 12, Reason = "Conflict on delete" });
        var task = Task.FromResult(new HttpResult(WebCodes.Status409Conflict, json));

        DuplicateMatch? duplicate = null;
        var result = await task.OnFailAsync(WebCodes.Status409Conflict, (DuplicateMatch? dup) => duplicate = dup);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(duplicate?.ExistingId, Is.EqualTo(12));
            Assert.That(duplicate?.Reason, Is.EqualTo("Conflict on delete"));
        }
    }

    [Test]
    public async Task OnFailSelectAsync_WhenStatusCodeMatches_RecoversWithMappedModel()
    {
        using var response = CreateJsonResponse(HttpStatusCode.Conflict, new DuplicateMatch {
            ExistingId = 303,
            Reason = "Existing record found"
        });

        var result = await HttpResult.FromResponseAsync<CustomerModel>(response, NJsonService.Instance)
            .OnFailSelectAsync(WebCodes.Status404NotFound, (ValidationError? _) => new CustomerModel { Id = -1, Name = "NotFoundFallback" })
            .OnFailSelectAsync(WebCodes.Status409Conflict, (DuplicateMatch? dup) => new CustomerModel {
                Id = dup?.ExistingId ?? 0,
                Name = "RecoveredFromConflict"
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(WebCodes.Status200OK));
            Assert.That(result.Model?.Id, Is.EqualTo(303));
            Assert.That(result.Model?.Name, Is.EqualTo("RecoveredFromConflict"));
        }
    }

    [Test]
    public async Task SelectAsync_PreservesHttpResultAndErrorContentOnFailure()
    {
        using var response = CreateJsonResponse(HttpStatusCode.Conflict, new DuplicateMatch {
            ExistingId = 500,
            Reason = "Preserved across SelectAsync"
        });

        DuplicateMatch? duplicate = null;

        var result = await HttpResult.FromResponseAsync<CustomerModel>(response, NJsonService.Instance)
            .SelectAsync(c => c.Name)
            .OnFailAsync(WebCodes.Status409Conflict, (DuplicateMatch? dup) => duplicate = dup);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(WebCodes.Status409Conflict));
            Assert.That(duplicate?.ExistingId, Is.EqualTo(500));
            Assert.That(duplicate?.Reason, Is.EqualTo("Preserved across SelectAsync"));
        }
    }

    [Test]
    public async Task MatchAsync_WithTypedErrorAndStatusCode_FoldsSuccessAndFailure()
    {
        using var conflictResponse = CreateJsonResponse(HttpStatusCode.Conflict, new DuplicateMatch {
            ExistingId = 88,
            Reason = "Already exists"
        });
        using var okResponse = CreateJsonResponse(HttpStatusCode.OK, new CustomerModel {
            Id = 10,
            Name = "Grace Hopper"
        });

        var conflictText = await HttpResult.FromResponseAsync<CustomerModel>(conflictResponse, NJsonService.Instance)
            .MatchAsync(
                c => $"Created:{c.Id}",
                (DuplicateMatch? dup, int status) => $"Conflict({status}):{dup?.ExistingId}");

        var okText = await HttpResult.FromResponseAsync<CustomerModel>(okResponse, NJsonService.Instance)
            .MatchAsync(
                c => $"Created:{c.Id}",
                (DuplicateMatch? dup, int status) => $"Conflict({status}):{dup?.ExistingId}");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(conflictText, Is.EqualTo("Conflict(409):88"));
            Assert.That(okText, Is.EqualTo("Created:10"));
        }
    }
}
