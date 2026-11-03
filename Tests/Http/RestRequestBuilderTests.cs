using CoreOne;
using CoreOne.Http;
using CoreOne.Http.Attributes;
using CoreOne.Http.Models;
using CoreOne.ODataBuilders;
using CoreOne.Services;
using System.Text;

namespace Tests.Http;

public class RestRequestBuilderTests
{
    public class UserFilter
    {
        [AliasAs("search_term")]
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public string? NullIgnored { get; set; }
    }

    public class LoginForm
    {
        [AliasAs("grant_type")]
        public string GrantType { get; set; } = "password";
        [AliasAs("username")]
        public string Username { get; set; } = "alice";
        [AliasAs("scopes")]
        public string[] Scopes { get; set; } = ["read", "write"];
    }

    [Test]
    public void Build_RouteAndCatchAllParameters_InterpolatesCorrectly()
    {
        var builder = new RestRequestBuilder("GET", "/api/v1", "/files/{category}/{*filePath}")
            .AddRouteParam("category", "reports & summaries")
            .AddRouteParam("filePath", "2026/q4/annual report.pdf");

        using var request = builder.Build();
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
        Assert.That(request.RequestUri?.OriginalString, Is.EqualTo("/api/v1/files/reports%20%26%20summaries/2026/q4/annual%20report.pdf"));
    }

    [Test]
    public void Build_QueryHttpVerb_UsesQueryMethodAndAppendsQueryParams()
    {
        var filter = new UserFilter { Search = "john doe", Page = 2 };
        var builder = new RestRequestBuilder("QUERY", "/api", "/users/search")
            .AddQueryParam("filter", filter)
            .AddQueryParam("tag", new[] { "admin", "active" })
            .AddQueryParam("ignored", null);

        using var request = builder.Build();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Method.Method, Is.EqualTo("QUERY"));
            Assert.That(request.RequestUri?.OriginalString, Is.EqualTo("/api/users/search?search_term=john%20doe&Page=2&tag=admin&tag=active"));
        }
    }

    [Test]
    public void Build_WithODataBuilderQueryParam_AppendsODataQuerySegment()
    {
        var odata = new ODataBuilder().Top(10).Skip(20).Select("Id,Name").Count();
        var builder = new RestRequestBuilder("GET", null, "/odata/Customers")
            .AddQueryParam("odata", odata)
            .AddQueryParam("custom", "true");

        using var request = builder.Build();
        Assert.That(request.RequestUri?.OriginalString, Is.EqualTo("/odata/Customers?$Count=true&$Skip=20&$Top=10&$Select=Id,Name&custom=true"));
    }

    [Test]
    public void Build_WithHeadersAndAuthorization_SetsRequestAndAuthHeaders()
    {
        var builder = new RestRequestBuilder("GET", "/api", "/me")
            .AddHeaders("X-Static: 123", "X-Override: old")
            .AddHeader("X-Override", "new")
            .AddHeader("X-Null", null)
            .WithAuthorization("Bearer", "secret-jwt-token");

        using var request = builder.Build();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Headers.GetValues("X-Static").Single(), Is.EqualTo("123"));
            Assert.That(request.Headers.GetValues("X-Override").Single(), Is.EqualTo("new"));
            Assert.That(request.Headers.Contains("X-Null"), Is.False);
            Assert.That(request.Headers.Authorization?.Scheme, Is.EqualTo("Bearer"));
            Assert.That(request.Headers.Authorization?.Parameter, Is.EqualTo("secret-jwt-token"));
        }
    }

    [Test]
    public async Task Build_WithUrlEncodedBody_EncodesFormFields()
    {
        var form = new LoginForm();
        var builder = new RestRequestBuilder("POST", "/oauth", "/token")
            .WithBody(form, NJsonService.Instance, BodySerializationMethod.UrlEncoded);

        using var request = builder.Build();
        Assert.That(request.Content, Is.Not.Null);
        var bodyStr = await request.Content!.ReadAsStringAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/x-www-form-urlencoded"));
            Assert.That(bodyStr, Does.Contain("grant_type=password"));
            Assert.That(bodyStr, Does.Contain("username=alice"));
            Assert.That(bodyStr, Does.Contain("scopes=read&scopes=write"));
        }
    }

    [Test]
    public async Task Build_WithMultipartItems_ConstructsMultipartFormDataContent()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello stream"));
        var streamPart = new StreamPart(stream, "greeting.txt", "text/plain");
        var bytePart = new ByteArrayPart([1, 2, 3, 4], "data.bin", "application/octet-stream");

        var builder = new RestRequestBuilder("POST", "/api", "/upload")
            .AsMultipart("----TestBoundary")
            .AddMultipartItem("file1", streamPart, NJsonService.Instance)
            .AddMultipartItem("file2", bytePart, NJsonService.Instance)
            .AddMultipartItem("description", "test upload", NJsonService.Instance);

        using var request = builder.Build();
        Assert.That(request.Content, Is.InstanceOf<MultipartFormDataContent>());
        var contentStr = await request.Content!.ReadAsStringAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(contentStr, Does.Contain("greeting.txt"));
            Assert.That(contentStr, Does.Contain("hello stream"));
            Assert.That(contentStr, Does.Contain("data.bin"));
            Assert.That(contentStr, Does.Contain("test upload"));
        }
    }

    [Test]
    public void AddHeaders_AuthorizationHeaderString_ParsedIntoAuthenticationHeader()
    {
        var builder = new RestRequestBuilder("GET", "/api", "/me")
            .AddHeaders("Authorization: Bearer abc123");

        using var request = builder.Build();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Headers.Authorization?.Scheme, Is.EqualTo("Bearer"));
            Assert.That(request.Headers.Authorization?.Parameter, Is.EqualTo("abc123"));
        }
    }

    [Test]
    public void WithAuthorization_EmptyScheme_DefaultsToBearer()
    {
        var builder = new RestRequestBuilder("GET", "/api", "/me")
            .WithAuthorization(string.Empty, "token-xyz");

        using var request = builder.Build();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Headers.Authorization?.Scheme, Is.EqualTo("Bearer"));
            Assert.That(request.Headers.Authorization?.Parameter, Is.EqualTo("token-xyz"));
        }
    }

    [Test]
    public async Task WithBody_SerializedBody_ProducesJsonContent()
    {
        var builder = new RestRequestBuilder("POST", "/api", "/users")
            .WithBody(new UserFilter { Search = "jane" }, NJsonService.Instance);

        using var request = builder.Build();
        var body = await request.Content!.ReadAsStringAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
            Assert.That(request.Content.Headers.ContentType?.CharSet, Is.EqualTo("utf-8"));
            Assert.That(body, Does.Contain("jane"));
        }
    }

    [Test]
    public void WithBody_HttpContentInstance_UsedAsIs()
    {
        using var raw = new StringContent("raw body", Encoding.UTF8, "text/plain");
        var builder = new RestRequestBuilder("POST", "/api", "/raw")
            .WithBody(raw, NJsonService.Instance);

        using var request = builder.Build();
        Assert.That(request.Content, Is.SameAs(raw));
    }

    [Test]
    public void WithBody_Stream_WrappedInStreamContent()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("stream body"));
        var builder = new RestRequestBuilder("POST", "/api", "/stream")
            .WithBody(stream, NJsonService.Instance);

        using var request = builder.Build();
        Assert.That(request.Content, Is.InstanceOf<StreamContent>());
    }

    [Test]
    public async Task AddMultipartItem_RawStreamBytesAndComplexObject_AddedAsParts()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("raw stream part"));
        var builder = new RestRequestBuilder("POST", "/api", "/upload")
            .AsMultipart()
            .AddMultipartItem("rawStream", stream, NJsonService.Instance)
            .AddMultipartItem("rawBytes", new byte[] { 65, 66, 67 }, NJsonService.Instance)
            .AddMultipartItem("filter", new UserFilter { Search = "complex" }, NJsonService.Instance);

        using var request = builder.Build();
        var contentStr = await request.Content!.ReadAsStringAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(contentStr, Does.Contain("raw stream part"));
            Assert.That(contentStr, Does.Contain("ABC"));
            Assert.That(contentStr, Does.Contain("complex"));
        }
    }

    [Test]
    public async Task AddMultipartItem_FileInfoPart_AddsFileContentWithName()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "file part content");
            var fileInfo = new FileInfo(tempFile);

            var builder = new RestRequestBuilder("POST", "/api", "/upload")
                .AddMultipartItem("document", new FileInfoPart(fileInfo, contentType: "text/plain"), NJsonService.Instance);

            using var request = builder.Build();
            var contentStr = await request.Content!.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(request.Content, Is.InstanceOf<MultipartFormDataContent>());
                Assert.That(contentStr, Does.Contain("file part content"));
                Assert.That(contentStr, Does.Contain(fileInfo.Name));
                Assert.That(contentStr, Does.Contain("text/plain"));
            }
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void Build_ContentScopedHeader_AssignedToContentHeaders()
    {
        var builder = new RestRequestBuilder("POST", "/api", "/users")
            .WithBody(new UserFilter { Search = "x" }, NJsonService.Instance)
            .AddHeader("Content-Language", "en-US");

        using var request = builder.Build();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Headers.Any(h => h.Key == "Content-Language"), Is.False);
            Assert.That(request.Content!.Headers.ContentLanguage, Does.Contain("en-US"));
        }
    }

    [Test]
    public void Build_UnknownHttpVerb_UsesCustomMethod()
    {
        var builder = new RestRequestBuilder("CUSTOM", "/api", "/thing");

        using var request = builder.Build();
        Assert.That(request.Method.Method, Is.EqualTo("CUSTOM"));
    }

    [Test]
    public void AddRouteParam_TokenNotInRoute_LeavesPathUnchanged()
    {
        var builder = new RestRequestBuilder("GET", "/api", "/users/{id}")
            .AddRouteParam("id", 5)
            .AddRouteParam("missing", "ignored");

        using var request = builder.Build();
        Assert.That(request.RequestUri?.OriginalString, Is.EqualTo("/api/users/5"));
    }

    [Test]
    public void AddQueryParam_EmptyODataBuilder_AddsNoQuerySegment()
    {
        var builder = new RestRequestBuilder("GET", null, "/odata/Customers")
            .AddQueryParam("odata", new ODataBuilder());

        using var request = builder.Build();
        Assert.That(request.RequestUri?.OriginalString, Is.EqualTo("/odata/Customers"));
    }

    [Test]
    public void AddQueryParam_KeyValuePairCollection_BindsEachPair()
    {
        var pairs = new List<KeyValuePair<string, string?>> {
            new("a", "1"),
            new("b", "two words"),
            new("skipped", null)
        };
        var builder = new RestRequestBuilder("GET", "/api", "/search")
            .AddQueryParam("unused", pairs);

        using var request = builder.Build();
        Assert.That(request.RequestUri?.OriginalString, Is.EqualTo("/api/search?a=1&b=two%20words"));
    }

    [Test]
    public void Build_AbsoluteRelativePath_IgnoresBasePath()
    {
        var builder = new RestRequestBuilder("GET", "/api/v1", "https://other.test/direct");

        using var request = builder.Build();
        Assert.That(request.RequestUri?.OriginalString, Is.EqualTo("https://other.test/direct"));
    }

    [Test]
    public void Build_QueryParamWithDictionaryAndDateFormatting_EncodesExpectedQueryString()
    {
        var dict = new Dictionary<string, int> { ["min"] = 5, ["max"] = 10 };
        var date = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);

        var builder = new RestRequestBuilder("PATCH", "https://api.example.com/v1", "/records")
            .AddQueryParam("bounds", dict)
            .AddQueryParam("date", date, "yyyy-MM-dd")
            .AddQueryParam("active", true);

        using var request = builder.Build();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Method.Method, Is.EqualTo("PATCH"));
            Assert.That(request.RequestUri?.ToString(), Is.EqualTo("https://api.example.com/v1/records?min=5&max=10&date=2026-11-02&active=true"));
        }
    }

    public sealed class SampleLookup(string code) : CoreOne.Lookups.LookupType<SampleLookup>(code)
    {
        public static readonly SampleLookup Active = new("active");
    }

    public sealed class PageQueryWithLookup : CoreOne.Operations.PageRequest
    {
        public SampleLookup? Status { get; set; }
    }

    [Test]
    public void Build_PageRequestWithLookupType_IgnoresTokenAndFormatsLookupAndOperations()
    {
        var query = new PageQueryWithLookup { CurrentPage = 2, PageSize = 15, Status = SampleLookup.Active };
        query.FilterBy("acme", "Name");
        query.OrderBy("CreatedAt", CoreOne.Operations.SortDirection.Descending);

        var builder = new RestRequestBuilder("GET", "/api", "/items")
            .AddQueryParam("query", query);

        using var request = builder.Build();
        Assert.That(request.RequestUri?.OriginalString, Is.EqualTo("/api/items?Status=active&CurrentPage=1&PageSize=15&Operations=Name%20acme&Operations=CreatedAt%20Descending"));
    }
}
