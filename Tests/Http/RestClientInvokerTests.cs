using CoreOne;
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
        using var client = CreateClient(HttpStatusCode.NotFound);

        var result = await RestClientInvoker.SendForHttpResultAsync(client, CreateRequest());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(404));
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
}
