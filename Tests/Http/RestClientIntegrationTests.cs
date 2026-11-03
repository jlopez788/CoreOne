using CoreOne;
using CoreOne.Attributes;
using CoreOne.Extensions;
using CoreOne.Http.Attributes;
using CoreOne.Http.Extensions;
using CoreOne.Http.Models;
using CoreOne.ODataBuilders;
using CoreOne.Results;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace Tests.Http;

[RestClient("/api/v1")]
[Headers("X-Api-Version: 1.0")]
public interface ICustomerApi
{
    [Get("/customers/{id}")]
    Task<HttpResult<CustomerDto>> GetCustomerAsync(int id, CancellationToken cancellationToken = default);

    [Post("/customers")]
    Task<HttpResult<CustomerDto, ProblemErrorDto>> CreateCustomerAsync([Body] CustomerDto customer, [Authorize] string token, CancellationToken cancellationToken = default);

    [Query("/customers/search")]
    Task<HttpResult<List<CustomerDto>>> SearchCustomersAsync([Body] CustomerSearchRequest queryBody, [QueryParam("odata")] ODataBuilder? odata = null, CancellationToken cancellationToken = default);

    [Delete("/customers/{id}")]
    Task<HttpResult> DeleteCustomerAsync(int id);

    [Get("/customers/{id}/raw")]
    Task<CustomerDto> GetCustomerRawAsync(int id);

    [Post("/customers/form")]
    Task<IResult<CustomerDto>> SubmitFormAsync([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> form);

    [Multipart]
    [Post("/customers/{id}/avatar")]
    Task<HttpResult<string>> UploadAvatarAsync(int id, StreamPart avatar, [AliasAs("caption")] string caption);

    [Serializer("xml")]
    [Post("/customers/xml")]
    Task<HttpResult<XmlCustomerDto>> EchoXmlCustomerAsync([Body] XmlCustomerDto customer);
}

[Intercept<TrackingInterceptor>]
[RestClient("/api/intercepted")]
public interface IInterceptedApi
{
    [Get("/items/{id}")]
    Task<HttpResult<CustomerDto>> GetItemAsync(int id);
}

[RestClient("/api/v2")]
public interface IExtrasApi
{
    [Put("/items/{id}")]
    Task<HttpResult<CustomerDto>> UpdateItemAsync(int id, [Body] CustomerDto item);

    [Patch("/items/{id}")]
    Task<HttpResult<CustomerDto>> PatchItemAsync(int id, [Body] CustomerDto item);

    [Head("/items/{id}")]
    Task<HttpResult> HeadItemAsync(int id);

    [Options("/items")]
    Task<IResult> OptionsItemsAsync();

    [Get("/items/{id}/name")]
    Task<string> GetItemNameAsync(int id);

    [Get("/items/{id}/data")]
    Task<byte[]> GetItemDataAsync(int id);

    [Get("/items/{id}/response")]
    Task<HttpResponseMessage> GetItemResponseAsync(int id);

    [Get("/items/{id}")]
    Task<HttpResult<CustomerDto>> GetItemWithHeaderAsync(int id, [Header("X-Correlation-Id")] string correlationId);

    [Serializer<XmlTestSerializer>]
    [Post("/items/xml")]
    Task<HttpResult<XmlCustomerDto>> EchoTypedXmlAsync([Body] XmlCustomerDto item);

    [Get("/items/{id}/result")]
    Task<Result<CustomerDto>> GetItemResultAsync(int id);
}

[RestClient("/named", "NamedThingClient")]
public interface INamedThingApi
{
    [Get("/thing")]
    Task<HttpResult<CustomerDto>> GetThingAsync();
}

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ProblemErrorDto
{
    public string Code { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public class CustomerSearchRequest
{
    public string Department { get; set; } = string.Empty;
}

[XmlRoot("Customer")]
public class XmlCustomerDto
{
    [XmlElement("Id")]
    public int Id { get; set; }

    [XmlElement("Name")]
    public string Name { get; set; } = string.Empty;
}

public class XmlTestSerializer : ISerializer
{
    public string ContentType => "application/xml";
    public Encoding Encoding => Encoding.UTF8;

    public IResult<object> Deserialize(byte[]? data, Type objectType)
    {
        if (data is null || data.Length == 0)
            return new Result<object>(null);

        using var ms = new MemoryStream(data);
        var xml = new XmlSerializer(objectType);
        return new Result<object>(xml.Deserialize(ms));
    }

    public Task<IResult<object>> DeserializeAsync(Stream stream, Type objectType, CancellationToken cancellationToken = default)
    {
        try
        {
            var xml = new XmlSerializer(objectType);
            var obj = xml.Deserialize(stream);
            return Task.FromResult<IResult<object>>(new Result<object>(obj));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.FromException<object>(ex));
        }
    }

    public byte[] Serialize(object? model, Type? objectType = null)
    {
        if (model is null)
            return [];

        using var ms = new MemoryStream();
        var xml = new XmlSerializer(objectType ?? model.GetType());
        xml.Serialize(ms, model);
        return ms.ToArray();
    }
}

public class TrackingInterceptor : IAsyncInterceptor
{
    public List<string> InvokedMethods { get; } = [];

    public async Task<object?> InterceptAsync(IInvocation invocation)
    {
        InvokedMethods.Add($"{invocation.MethodName}({string.Join(",", invocation.Arguments)})");
        return await invocation.ProceedAsync();
    }
}

public class RestClientIntegrationTests
{
    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }

    [Test]
    public async Task GetCustomerAsync_ReturnsDeserializedJsonHttpResult()
    {
        HttpRequestMessage? capturedRequest = null;
        var services = new ServiceCollection();
        services.AddCoreServices();
        services.AddRestClient<ICustomerApi>(client => client.BaseAddress = new Uri("https://example.test"))
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler((req, _) => {
                capturedRequest = new HttpRequestMessage(req.Method, req.RequestUri);
                foreach (var h in req.Headers)
                    capturedRequest.Headers.TryAddWithoutValidation(h.Key, h.Value);

                var json = Utility.Serialize(new CustomerDto { Id = 42, Name = "Ada Lovelace" });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }));

        using var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<ICustomerApi>();

        var result = await api.GetCustomerAsync(42);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Model?.Id, Is.EqualTo(42));
            Assert.That(result.Model?.Name, Is.EqualTo("Ada Lovelace"));
            Assert.That(capturedRequest?.RequestUri?.ToString(), Is.EqualTo("https://example.test/api/v1/customers/42"));
            Assert.That(capturedRequest?.Headers.GetValues("X-Api-Version").Single(), Is.EqualTo("1.0"));
        }
    }

    [Test]
    public async Task CreateCustomerAsync_WhenBadRequest_DeserializesErrorModel()
    {
        string? authHeader = null;
        var services = new ServiceCollection();
        services.AddCoreServices();
        services.AddRestClient<ICustomerApi>(client => client.BaseAddress = new Uri("https://example.test"))
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler((req, _) => {
                authHeader = req.Headers.Authorization?.ToString();
                var errorJson = Utility.Serialize(new ProblemErrorDto { Code = "DUPLICATE", Detail = "Customer already exists" });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest) {
                    Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
                });
            }));

        using var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<ICustomerApi>();

        var response = await api.CreateCustomerAsync(new CustomerDto { Id = 1, Name = "Duplicate" }, "my-token-123");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Success, Is.False);
            Assert.That(response.StatusCode, Is.EqualTo(400));
            Assert.That(response.ErrorModel?.Code, Is.EqualTo("DUPLICATE"));
            Assert.That(response.ErrorModel?.Detail, Is.EqualTo("Customer already exists"));
            Assert.That(authHeader, Is.EqualTo("Bearer my-token-123"));
        }
    }

    [Test]
    public async Task SearchCustomersAsync_HttpQueryVerbWithBodyAndOData_SendsQueryMethodAndBody()
    {
        string? methodUsed = null;
        string? requestUri = null;
        string? requestBody = null;

        var services = new ServiceCollection();
        services.AddCoreServices();
        services.AddRestClient<ICustomerApi>(client => client.BaseAddress = new Uri("https://example.test"))
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler(async (req, _) => {
                methodUsed = req.Method.Method;
                requestUri = req.RequestUri?.ToString();
                requestBody = req.Content is not null ? await req.Content.ReadAsStringAsync() : null;

                var listJson = Utility.Serialize(new List<CustomerDto> { new() { Id = 7, Name = "Grace Hopper" } });
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(listJson, Encoding.UTF8, "application/json")
                };
            }));

        using var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<ICustomerApi>();

        var odata = new ODataBuilder().Top(5).Skip(0);
        var result = await api.SearchCustomersAsync(new CustomerSearchRequest { Department = "Engineering" }, odata);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Has.Count.EqualTo(1));
            Assert.That(methodUsed, Is.EqualTo("QUERY"));
            Assert.That(requestUri, Is.EqualTo("https://example.test/api/v1/customers/search?$Skip=0&$Top=5"));
            Assert.That(requestBody, Does.Contain("Engineering"));
        }
    }

    [Test]
    public async Task EchoXmlCustomerAsync_UsesKeyedXmlSerializerForRequestAndResponse()
    {
        string? requestContentType = null;
        string? requestXml = null;

        var services = new ServiceCollection();
        services.AddCoreServices();
        services.AddKeyedSingleton<ISerializer, XmlTestSerializer>("xml");
        services.AddRestClient<ICustomerApi>(client => client.BaseAddress = new Uri("https://example.test"))
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler(async (req, _) => {
                requestContentType = req.Content?.Headers.ContentType?.MediaType;
                requestXml = req.Content is not null ? await req.Content.ReadAsStringAsync() : null;

                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(requestXml ?? string.Empty, Encoding.UTF8, "application/xml")
                };
            }));

        using var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<ICustomerApi>();

        var result = await api.EchoXmlCustomerAsync(new XmlCustomerDto { Id = 99, Name = "Alan Turing" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(requestContentType, Is.EqualTo("application/xml"));
            Assert.That(requestXml, Does.Contain("<Customer"));
            Assert.That(result.Model?.Id, Is.EqualTo(99));
            Assert.That(result.Model?.Name, Is.EqualTo("Alan Turing"));
        }
    }

    [Test]
    public async Task InterceptedApi_ResolvesTwoStageProxyAndExecutesInterceptorPipeline()
    {
        var services = new ServiceCollection();
        services.AddCoreServices();
        services.AddSingleton<TrackingInterceptor>();
        services.AddRestClient<IInterceptedApi>(client => client.BaseAddress = new Uri("https://example.test"))
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler((_, _) => {
                var json = Utility.Serialize(new CustomerDto { Id = 55, Name = "Intercepted User" });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }));

        using var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<IInterceptedApi>();
        var interceptor = sp.GetRequiredService<TrackingInterceptor>();

        var result = await api.GetItemAsync(55);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(api.GetType().Name, Is.EqualTo("InterceptedApiClientProxy"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model?.Id, Is.EqualTo(55));
            Assert.That(interceptor.InvokedMethods, Has.Count.EqualTo(1));
            Assert.That(interceptor.InvokedMethods[0], Is.EqualTo("GetItemAsync(55)"));
        }
    }

    [Test]
    public async Task UploadAvatarAndRawAndFormMethods_ExecuteExpectedHttpCalls()
    {
        var services = new ServiceCollection();
        services.AddCoreServices();
        services.AddRestClient<ICustomerApi>(client => client.BaseAddress = new Uri("https://example.test"))
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler(async (req, _) => {
                if (req.RequestUri!.AbsolutePath.EndsWith("/avatar"))
                {
                    var body = await req.Content!.ReadAsStringAsync();
                    return new HttpResponseMessage(HttpStatusCode.OK) {
                        Content = new StringContent(body.Contains("profile-pic.png") ? "uploaded" : "missing", Encoding.UTF8, "text/plain")
                    };
                }

                if (req.RequestUri.AbsolutePath.EndsWith("/form"))
                {
                    var formBody = await req.Content!.ReadAsStringAsync();
                    var name = formBody.Contains("role=admin") ? "FormAdmin" : "Unknown";
                    return new HttpResponseMessage(HttpStatusCode.OK) {
                        Content = new StringContent(Utility.Serialize(new CustomerDto { Id = 10, Name = name }), Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(Utility.Serialize(new CustomerDto { Id = 88, Name = "RawModel" }), Encoding.UTF8, "application/json")
                };
            }));

        using var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<ICustomerApi>();

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("png-bytes"));
        var uploadResult = await api.UploadAvatarAsync(10, new StreamPart(ms, "profile-pic.png", "image/png"), "My Avatar");
        var formResult = await api.SubmitFormAsync(new Dictionary<string, string> { ["role"] = "admin" });
        var rawModel = await api.GetCustomerRawAsync(88);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(uploadResult.Success, Is.True);
            Assert.That(uploadResult.Model, Is.EqualTo("uploaded"));
            Assert.That(formResult.Success, Is.True);
            Assert.That(formResult.Model?.Name, Is.EqualTo("FormAdmin"));
            Assert.That(rawModel.Id, Is.EqualTo(88));
            Assert.That(rawModel.Name, Is.EqualTo("RawModel"));
        }
    }

    private static ServiceProvider BuildProvider<TApi>(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        where TApi : class
    {
        var services = new ServiceCollection();
        services.AddCoreServices();
        services.AddRestClient<TApi>(client => client.BaseAddress = new Uri("https://example.test"))
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler(handler));
        return services.BuildServiceProvider();
    }

    [Test]
    public async Task UpdateAndPatchItem_SendPutAndPatchVerbs()
    {
        var methods = new List<string>();
        using var sp = BuildProvider<IExtrasApi>((req, _) => {
            methods.Add(req.Method.Method);
            var json = Utility.Serialize(new CustomerDto { Id = 1, Name = "Updated" });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });
        var api = sp.GetRequiredService<IExtrasApi>();

        var putResult = await api.UpdateItemAsync(1, new CustomerDto { Id = 1, Name = "Updated" });
        var patchResult = await api.PatchItemAsync(1, new CustomerDto { Id = 1, Name = "Patched" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(putResult.Success, Is.True);
            Assert.That(patchResult.Success, Is.True);
            Assert.That(methods, Is.EqualTo(new[] { "PUT", "PATCH" }));
        }
    }

    [Test]
    public async Task HeadAndOptionsItem_SendExpectedVerbsAndReturnResults()
    {
        var methods = new List<string>();
        using var sp = BuildProvider<IExtrasApi>((req, _) => {
            methods.Add(req.Method.Method);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        var api = sp.GetRequiredService<IExtrasApi>();

        var headResult = await api.HeadItemAsync(9);
        var optionsResult = await api.OptionsItemsAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(headResult.Success, Is.True);
            Assert.That(optionsResult.Success, Is.True);
            Assert.That(methods, Is.EqualTo(new[] { "HEAD", "OPTIONS" }));
        }
    }

    [Test]
    public async Task RawReturnTypes_StringBytesAndResponse_ReturnUnprocessedContent()
    {
        using var sp = BuildProvider<IExtrasApi>((req, _) => {
            var path = req.RequestUri!.AbsolutePath;
            HttpContent content = path.EndsWith("/data")
                ? new ByteArrayContent([10, 20, 30])
                : new StringContent("plain name", Encoding.UTF8, "text/plain");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        });
        var api = sp.GetRequiredService<IExtrasApi>();

        var name = await api.GetItemNameAsync(1);
        var data = await api.GetItemDataAsync(1);
        using var response = await api.GetItemResponseAsync(1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(name, Is.EqualTo("plain name"));
            Assert.That(data, Is.EqualTo(new byte[] { 10, 20, 30 }));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }

    [Test]
    public async Task GetItemWithHeaderAsync_HeaderParameter_AddsDynamicHeader()
    {
        string? correlationId = null;
        using var sp = BuildProvider<IExtrasApi>((req, _) => {
            correlationId = req.Headers.TryGetValues("X-Correlation-Id", out var values) ? values.Single() : null;
            var json = Utility.Serialize(new CustomerDto { Id = 3, Name = "WithHeader" });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });
        var api = sp.GetRequiredService<IExtrasApi>();

        var result = await api.GetItemWithHeaderAsync(3, "corr-123");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(correlationId, Is.EqualTo("corr-123"));
        }
    }

    [Test]
    public async Task EchoTypedXmlAsync_TypedSerializerAttribute_UsesXmlSerializer()
    {
        string? requestContentType = null;
        using var sp = BuildProvider<IExtrasApi>(async (req, _) => {
            requestContentType = req.Content?.Headers.ContentType?.MediaType;
            var xml = req.Content is not null ? await req.Content.ReadAsStringAsync() : string.Empty;
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(xml, Encoding.UTF8, "application/xml")
            };
        });
        var api = sp.GetRequiredService<IExtrasApi>();

        var result = await api.EchoTypedXmlAsync(new XmlCustomerDto { Id = 21, Name = "Typed Xml" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(requestContentType, Is.EqualTo("application/xml"));
            Assert.That(result.Model?.Id, Is.EqualTo(21));
            Assert.That(result.Model?.Name, Is.EqualTo("Typed Xml"));
        }
    }

    [Test]
    public async Task GetItemResultAsync_ResultReturnType_MapsSuccessAndFailure()
    {
        var fail = false;
        using var sp = BuildProvider<IExtrasApi>((_, _) => {
            if (fail)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

            var json = Utility.Serialize(new CustomerDto { Id = 8, Name = "AsResult" });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });
        var api = sp.GetRequiredService<IExtrasApi>();

        var success = await api.GetItemResultAsync(8);
        fail = true;
        var failure = await api.GetItemResultAsync(8);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(success.Success, Is.True);
            Assert.That(success.Model?.Name, Is.EqualTo("AsResult"));
            Assert.That(failure.Success, Is.False);
        }
    }

    [Test]
    public async Task NamedThingApi_CustomClientName_GeneratesAndResolvesNamedClient()
    {
        string? requestUri = null;
        using var sp = BuildProvider<INamedThingApi>((req, _) => {
            requestUri = req.RequestUri?.ToString();
            var json = Utility.Serialize(new CustomerDto { Id = 77, Name = "Named" });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });
        var api = sp.GetRequiredService<INamedThingApi>();

        var result = await api.GetThingAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(api.GetType().Name, Is.EqualTo("NamedThingClient"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model?.Id, Is.EqualTo(77));
            Assert.That(requestUri, Is.EqualTo("https://example.test/named/thing"));
        }
    }

    [Test]
    public void AddRestClientsFromAssembly_RegistersAllGeneratedClientsInAssembly()
    {
        var services = new ServiceCollection();
        services.AddCoreServices();
        services.AddRestClientsFromAssembly<RestClientIntegrationTests>((_, client) => {
            client.BaseAddress = new Uri("https://example.test");
        });

        using var sp = services.BuildServiceProvider();
        var customerApi = sp.GetService<ICustomerApi>();
        var interceptedApi = sp.GetService<IInterceptedApi>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(customerApi, Is.Not.Null);
            Assert.That(interceptedApi, Is.Not.Null);
            Assert.That(interceptedApi!.GetType().Name, Is.EqualTo("InterceptedApiClientProxy"));
        }
    }
}
