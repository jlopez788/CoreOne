using CoreOne;
using CoreOne.Extensions;
using CoreOne.Http;
using CoreOne.Results;
using CoreOne.Services;
using System.Net;
using System.Text;

namespace Tests.Http;

public class RestClientInvokerTests
{
    public class ItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }

    private static HttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
        new(new MockHttpMessageHandler(handler)) { BaseAddress = new Uri("https://example.test") };

    private static HttpClient CreateClient(HttpStatusCode statusCode, HttpContent? content = null) =>
        CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(statusCode) {
            Content = content ?? new StringContent(string.Empty)
        }));

    private static HttpRequestMessage CreateRequest() => new(HttpMethod.Get, "/api/items");

    private static StringContent JsonContent(object model) =>
        new(Utility.Serialize(model), Encoding.UTF8, "application/json");

    [Test]
    public void SendAsync_SuccessResponse_Completes()
    {
        using var client = CreateClient(HttpStatusCode.OK);

        Assert.DoesNotThrowAsync(() => RestClientInvoker.SendAsync(client, CreateRequest()));
    }

    [Test]
    public void SendAsync_ErrorStatus_ThrowsHttpRequestException()
    {
        using var client = CreateClient(HttpStatusCode.InternalServerError);

        Assert.ThrowsAsync<HttpRequestException>(() => RestClientInvoker.SendAsync(client, CreateRequest()));
    }

    [Test]
    public async Task SendForHttpResultAsync_ErrorStatus_ReturnsFailureWithoutThrowing()
    {
        using var client = CreateClient(HttpStatusCode.NotFound, new StringContent("item missing"));

        var result = await RestClientInvoker.SendForHttpResultAsync(client, CreateRequest());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(404));
            Assert.That(result.Message, Is.EqualTo("item missing"));
        }
    }

    [Test]
    public async Task SendForHttpResultAsync_HandlerThrows_ReturnsExceptionResult()
    {
        using var client = CreateClient((_, _) => throw new InvalidOperationException("boom"));

        var result = await RestClientInvoker.SendForHttpResultAsync(client, CreateRequest());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(500));
            Assert.That(result.Message, Does.Contain("boom"));
        }
    }

    [Test]
    public async Task SendForHttpResultAsyncOfT_HandlerThrows_ReturnsExceptionResult()
    {
        using var client = CreateClient((_, _) => throw new InvalidOperationException("model boom"));

        var result = await RestClientInvoker.SendForHttpResultAsync<ItemDto>(client, CreateRequest(), NJsonService.Instance);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(500));
            Assert.That(result.Model, Is.Null);
            Assert.That(result.Message, Does.Contain("model boom"));
        }
    }

    [Test]
    public async Task SendForHttpResultAsyncWithError_HandlerThrows_ReturnsExceptionResult()
    {
        using var client = CreateClient((_, _) => throw new InvalidOperationException("error boom"));

        var result = await RestClientInvoker.SendForHttpResultAsync<ItemDto, ItemDto>(client, CreateRequest(), NJsonService.Instance);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(500));
            Assert.That(result.Message, Does.Contain("error boom"));
        }
    }

    [Test]
    public async Task SendForModelAsync_StringModel_ReturnsRawContent()
    {
        using var client = CreateClient(HttpStatusCode.OK, new StringContent("plain text body", Encoding.UTF8, "text/plain"));

        var result = await RestClientInvoker.SendForModelAsync<string>(client, CreateRequest(), NJsonService.Instance);

        Assert.That(result, Is.EqualTo("plain text body"));
    }

    [Test]
    public async Task SendForModelAsync_ByteArrayModel_ReturnsRawBytes()
    {
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        using var client = CreateClient(HttpStatusCode.OK, new ByteArrayContent(payload));

        var result = await RestClientInvoker.SendForModelAsync<byte[]>(client, CreateRequest(), NJsonService.Instance);

        Assert.That(result, Is.EqualTo(payload));
    }

    [Test]
    public async Task SendForModelAsync_HttpResponseMessageModel_ReturnsRawResponseEvenOnFailure()
    {
        using var client = CreateClient((HttpStatusCode)418, new StringContent("teapot"));

        using var response = await RestClientInvoker.SendForModelAsync<HttpResponseMessage>(client, CreateRequest(), NJsonService.Instance);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response, Is.Not.Null);
            Assert.That((int)response!.StatusCode, Is.EqualTo(418));
        }
    }

    [Test]
    public async Task SendForModelAsync_JsonModel_Deserializes()
    {
        using var client = CreateClient(HttpStatusCode.OK, JsonContent(new ItemDto { Id = 7, Name = "Widget" }));

        var model = await RestClientInvoker.SendForModelAsync<ItemDto>(client, CreateRequest(), NJsonService.Instance);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model?.Id, Is.EqualTo(7));
            Assert.That(model?.Name, Is.EqualTo("Widget"));
        }
    }

    [Test]
    public void SendForModelAsync_ErrorStatus_ThrowsHttpRequestException()
    {
        using var client = CreateClient(HttpStatusCode.BadRequest);

        Assert.ThrowsAsync<HttpRequestException>(() =>
            RestClientInvoker.SendForModelAsync<ItemDto>(client, CreateRequest(), NJsonService.Instance));
    }

    [Test]
    public void SendForModelAsync_MalformedJson_ThrowsInvalidOperationException()
    {
        using var client = CreateClient(HttpStatusCode.OK, new StringContent("{ not valid json !!!", Encoding.UTF8, "application/json"));

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            RestClientInvoker.SendForModelAsync<ItemDto>(client, CreateRequest(), NJsonService.Instance));
    }

    [Test]
    public async Task SendForIResultAsync_SuccessResponse_ReturnsSuccessResult()
    {
        using var client = CreateClient(HttpStatusCode.OK);

        var result = await RestClientInvoker.SendForIResultAsync(client, CreateRequest());

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task SendForIResultAsyncOfT_SuccessResponse_ReturnsModel()
    {
        using var client = CreateClient(HttpStatusCode.OK, JsonContent(new ItemDto { Id = 3, Name = "Gear" }));

        var result = await RestClientInvoker.SendForIResultAsync<ItemDto>(client, CreateRequest(), NJsonService.Instance);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model?.Id, Is.EqualTo(3));
        }
    }

    [Test]
    public async Task SendForResultAsync_ErrorStatus_ReturnsFailResult()
    {
        using var client = CreateClient(HttpStatusCode.NotFound);

        var result = await RestClientInvoker.SendForResultAsync(client, CreateRequest());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Fail));
        }
    }

    [Test]
    public async Task SendForResultAsyncOfT_SuccessResponse_ReturnsModel()
    {
        using var client = CreateClient(HttpStatusCode.OK, JsonContent(new ItemDto { Id = 11, Name = "Bolt" }));

        var result = await RestClientInvoker.SendForResultAsync<ItemDto>(client, CreateRequest(), NJsonService.Instance);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model?.Name, Is.EqualTo("Bolt"));
        }
    }

    [Test]
    public async Task SendForHttpResultAsync_PreCancelledToken_ReturnsExceptionResult()
    {
        using var client = CreateClient((_, ct) => {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await RestClientInvoker.SendForHttpResultAsync(client, CreateRequest(), cts.Token);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(500));
        }
    }

    [Test]
    public void SendForModelAsync_PreCancelledToken_ThrowsOperationCanceled()
    {
        using var client = CreateClient((_, ct) => {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.CatchAsync<OperationCanceledException>(() =>
            RestClientInvoker.SendForModelAsync<ItemDto>(client, CreateRequest(), NJsonService.Instance, cts.Token));
    }

    [Test]
    public void SendAsync_PreCancelledToken_ThrowsOperationCanceled()
    {
        using var client = CreateClient((_, ct) => {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.CatchAsync<OperationCanceledException>(() =>
            RestClientInvoker.SendAsync(client, CreateRequest(), cts.Token));
    }

    [Test]
    public async Task SendAsync_ActiveToken_ForwardsCancellationTokenToHandler()
    {
        var observedToken = CancellationToken.None;
        using var client = CreateClient((_, ct) => {
            observedToken = ct;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var cts = new CancellationTokenSource();

        await RestClientInvoker.SendAsync(client, CreateRequest(), cts.Token);

        Assert.That(observedToken.CanBeCanceled, Is.True);
    }

    [Test]
    public async Task SendForHttpResultAsyncOfT_PreCancelledToken_ReturnsExceptionResult()
    {
        using var client = CreateClient((_, ct) => {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await RestClientInvoker.SendForHttpResultAsync<ItemDto>(client, CreateRequest(), NJsonService.Instance, cts.Token);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(500));
            Assert.That(result.Model, Is.Null);
        }
    }

    [Test]
    public async Task SendForHttpResultAsyncWithError_PreCancelledToken_ReturnsExceptionResult()
    {
        using var client = CreateClient((_, ct) => {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await RestClientInvoker.SendForHttpResultAsync<ItemDto, ItemDto>(client, CreateRequest(), NJsonService.Instance, cts.Token);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(500));
            Assert.That(result.Model, Is.Null);
            Assert.That(result.ErrorModel, Is.Null);
        }
    }

    [Test]
    public async Task SendForIResultAsync_PreCancelledToken_ReturnsFailureResult()
    {
        using var client = CreateClient((_, ct) => {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await RestClientInvoker.SendForIResultAsync(client, CreateRequest(), cts.Token);

        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task SendForIResultAsyncOfT_PreCancelledToken_ReturnsFailureResult()
    {
        using var client = CreateClient((_, ct) => {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await RestClientInvoker.SendForIResultAsync<ItemDto>(client, CreateRequest(), NJsonService.Instance, cts.Token);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Model, Is.Null);
        }
    }

    [Test]
    public async Task SendForResultAsync_PreCancelledToken_ReturnsExceptionResult()
    {
        using var client = CreateClient((_, ct) => {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await RestClientInvoker.SendForResultAsync(client, CreateRequest(), cts.Token);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Exception));
        }
    }

    [Test]
    public async Task SendForResultAsyncOfT_PreCancelledToken_ReturnsExceptionResult()
    {
        using var client = CreateClient((_, ct) => {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await RestClientInvoker.SendForResultAsync<ItemDto>(client, CreateRequest(), NJsonService.Instance, cts.Token);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Exception));
            Assert.That(result.Model, Is.Null);
        }
    }

    [Test]
    public async Task SendForModelAsync_ForwardsCancellationTokenToSerializer()
    {
        var serializerToken = CancellationToken.None;
        var trackingSerializer = new TokenTrackingSerializer(ct => serializerToken = ct);
        using var client = CreateClient(HttpStatusCode.OK, JsonContent(new ItemDto { Id = 1, Name = "Tracked" }));
        using var cts = new CancellationTokenSource();

        var model = await RestClientInvoker.SendForModelAsync<ItemDto>(client, CreateRequest(), trackingSerializer, cts.Token);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(model?.Id, Is.EqualTo(1));
            Assert.That(serializerToken, Is.EqualTo(cts.Token));
        }
    }

    [Test]
    public async Task SendForHttpResultAsyncOfT_ForwardsCancellationTokenToSerializer()
    {
        var serializerToken = CancellationToken.None;
        var trackingSerializer = new TokenTrackingSerializer(ct => serializerToken = ct);
        using var client = CreateClient(HttpStatusCode.OK, JsonContent(new ItemDto { Id = 2, Name = "TrackedResult" }));
        using var cts = new CancellationTokenSource();

        var result = await RestClientInvoker.SendForHttpResultAsync<ItemDto>(client, CreateRequest(), trackingSerializer, cts.Token);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model?.Id, Is.EqualTo(2));
            Assert.That(serializerToken, Is.EqualTo(cts.Token));
        }
    }

    [Test]
    public async Task SendForHttpResultAsyncWithError_ForwardsCancellationTokenToSerializerOnError()
    {
        var serializerToken = CancellationToken.None;
        var trackingSerializer = new TokenTrackingSerializer(ct => serializerToken = ct);
        using var client = CreateClient(HttpStatusCode.BadRequest, JsonContent(new ItemDto { Id = 99, Name = "ErrorPayload" }));
        using var cts = new CancellationTokenSource();

        var result = await RestClientInvoker.SendForHttpResultAsync<ItemDto, ItemDto>(client, CreateRequest(), trackingSerializer, cts.Token);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorModel?.Id, Is.EqualTo(99));
            Assert.That(serializerToken, Is.EqualTo(cts.Token));
        }
    }

    [Test]
    public void SendForModelAsync_CancelledDuringInFlightRequest_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        using var client = CreateClient(async (_, ct) => {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        cts.CancelAfter(30);

        Assert.CatchAsync<OperationCanceledException>(() =>
            RestClientInvoker.SendForModelAsync<ItemDto>(client, CreateRequest(), NJsonService.Instance, cts.Token));
    }

    [Test]
    public async Task SendForHttpResultAsyncOfT_ConflictWithErrorBody_SupportsFluentOnFailMapping()
    {
        using var client = CreateClient(HttpStatusCode.Conflict, JsonContent(new ItemDto { Id = 409, Name = "DuplicateItem" }));
        ItemDto? duplicate = null;

        var result = await RestClientInvoker.SendForHttpResultAsync<ItemDto>(client, CreateRequest(), NJsonService.Instance)
            .OnFailAsync(WebCodes.Status409Conflict, (ItemDto? err) => duplicate = err);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(WebCodes.Status409Conflict));
            Assert.That(duplicate, Is.Not.Null);
            Assert.That(duplicate?.Id, Is.EqualTo(409));
            Assert.That(duplicate?.Name, Is.EqualTo("DuplicateItem"));
        }
    }

    private sealed class TokenTrackingSerializer(Action<CancellationToken> onDeserialize) : ISerializer
    {
        public string ContentType => "application/json";
        public Encoding Encoding => Encoding.UTF8;

        public IResult<object> Deserialize(byte[]? data, Type objectType) =>
            NJsonService.Instance.Deserialize(data, objectType);

        public Task<IResult<object>> DeserializeAsync(Stream stream, Type objectType, CancellationToken cancellationToken = default)
        {
            onDeserialize(cancellationToken);
            return NJsonService.Instance.DeserializeAsync(stream, objectType, cancellationToken);
        }

        public byte[] Serialize(object? model, Type? objectType = null) =>
            NJsonService.Instance.Serialize(model, objectType);
    }
}
