using CoreOne.Attributes;
using CoreOne.Generators.Rest;
using CoreOne.Http.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace Tests.Generators;

public class RestClientGeneratorTests
{
    private const string CommonUsings = """
        using System;
        using System.Collections.Generic;
        using System.Net.Http;
        using System.Threading;
        using System.Threading.Tasks;
        using CoreOne;
        using CoreOne.Attributes;
        using CoreOne.Http;
        using CoreOne.Http.Attributes;
        using CoreOne.Http.Models;
        using CoreOne.ODataBuilders;
        using CoreOne.Results;

        public record UserDto(int Id, string Name);
        public record ApiError(string Code, string Message);

        public class TestInterceptor : IAsyncInterceptor
        {
            public Task<object?> InterceptAsync(IInvocation invocation) => invocation.ProceedAsync();
        }
        """;

    private static Compilation CreateCompilation(string source)
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>();

        var coreOneRef = MetadataReference.CreateFromFile(typeof(InterceptAttribute).Assembly.Location);
        var coreOneHttpRef = MetadataReference.CreateFromFile(typeof(GetAttribute).Assembly.Location);

        var references = loadedAssemblies
            .Append(coreOneRef)
            .Append(coreOneHttpRef)
            .Distinct()
            .ToArray();

        return CSharpCompilation.Create(
            "TestRestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static (ImmutableArray<GeneratedSourceResult> Sources, Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(string source)
    {
        var compilation = CreateCompilation(source);
        var generator = new RestClientGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        var results = driver.GetRunResult().Results[0].GeneratedSources;
        return (results, outputCompilation, diagnostics);
    }

    [Test]
    public void Generator_PlainInterfaceWithoutHttpAttributes_ProducesNoOutput()
    {
        const string source = CommonUsings + """
            public interface IPlainService
            {
                Task<string> GetValueAsync();
            }
            """;

        var (sources, _, _) = RunGenerator(source);
        Assert.That(sources, Is.Empty);
    }

    [Test]
    public void Generator_InterfaceWithHttpVerbs_GeneratesClientWithZeroDiagnostics()
    {
        const string source = CommonUsings + """
            namespace MyApp.Clients
            {
                [RestClient("/api/v1")]
                [Headers("X-Client: CoreOne", "Accept: application/json")]
                public interface IUserApi
                {
                    [Get("/users/{id}")]
                    Task<HttpResult<UserDto>> GetUserAsync(int id, CancellationToken cancellationToken = default);

                    [Post("/users")]
                    Task<HttpResult<UserDto, ApiError>> CreateUserAsync([Body] UserDto user, CancellationToken cancellationToken = default);

                    [Put("/users/{id}")]
                    Task<IResult<UserDto>> UpdateUserAsync(int id, UserDto user);

                    [Delete("/users/{id}")]
                    Task<HttpResult> DeleteUserAsync(int id);

                    [Patch("/users/{id}")]
                    Task<UserDto> PatchUserAsync(int id, [Body] UserDto user);

                    [Head("/users/{id}")]
                    Task HeadUserAsync(int id);

                    [Options("/users")]
                    Task<IResult> OptionsUsersAsync();

                    [Query("/users/query")]
                    Task<HttpResult<List<UserDto>>> QueryUsersAsync([Body] object filter, [QueryParam("page")] int pageNumber = 1);
                }
            }
            """;

        var (sources, outputCompilation, genDiagnostics) = RunGenerator(source);
        var compilerErrors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(genDiagnostics, Is.Empty);
            Assert.That(compilerErrors, Is.Empty, () => string.Join(Environment.NewLine, compilerErrors));
            Assert.That(sources, Has.Length.EqualTo(1));
            Assert.That(sources[0].HintName, Is.EqualTo("UserApiClient.g.cs"));
        }

        var generatedCode = sources[0].SourceText.ToString();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(generatedCode, Does.Contain("public partial class UserApiClient : MyApp.Clients.IUserApi"));
            Assert.That(generatedCode, Does.Contain("new global::CoreOne.Http.RestRequestBuilder(\"GET\", \"/api/v1\", \"/users/{id}\")"));
            Assert.That(generatedCode, Does.Contain("new global::CoreOne.Http.RestRequestBuilder(\"QUERY\", \"/api/v1\", \"/users/query\")"));
            Assert.That(generatedCode, Does.Contain("_builder.AddQueryParam(\"page\", pageNumber, null);"));
            Assert.That(generatedCode, Does.Contain("SendForHttpResultAsync<UserDto>(HttpClient, _request, Serializer, cancellationToken)"));
            Assert.That(generatedCode, Does.Contain("SendForHttpResultAsync<UserDto, ApiError>(HttpClient, _request, Serializer, cancellationToken)"));
            Assert.That(generatedCode, Does.Contain("SendForHttpResultAsync(HttpClient, _request, global::System.Threading.CancellationToken.None)"));
            Assert.That(generatedCode, Does.Not.Contain("AddQueryParam(\"cancellationToken\""));
        }
    }

    [Test]
    public void Generator_WithInterceptAttribute_GeneratesTwoStageClientAndProxyWithZeroDiagnostics()
    {
        const string source = CommonUsings + """
            namespace MyApp.Clients
            {
                [Intercept<TestInterceptor>]
                public interface IOrderApi
                {
                    [Get("/orders/{orderId}")]
                    Task<HttpResult<UserDto>> GetOrderAsync([AliasAs("orderId")] int id, [Header("X-Correlation-Id")] string correlationId, [Authorize] string token);
                }
            }
            """;

        var (sources, outputCompilation, genDiagnostics) = RunGenerator(source);
        var compilerErrors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(genDiagnostics, Is.Empty);
            Assert.That(compilerErrors, Is.Empty, () => string.Join(Environment.NewLine, compilerErrors));
            Assert.That(sources, Has.Length.EqualTo(2));
            Assert.That(sources[0].HintName, Is.EqualTo("OrderApiClient.g.cs"));
            Assert.That(sources[1].HintName, Is.EqualTo("OrderApiClientProxy.g.cs"));
        }

        var proxyCode = sources[1].SourceText.ToString();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(proxyCode, Does.Contain("public partial class OrderApiClientProxy : OrderApiClient"));
            Assert.That(proxyCode, Does.Contain("public override async System.Threading.Tasks.Task<CoreOne.Results.HttpResult<UserDto>> GetOrderAsync"));
            Assert.That(proxyCode, Does.Contain("return await base.GetOrderAsync(id, correlationId, token);"));
        }
    }

    [Test]
    public void Generator_MultipartAndSerializerAttributes_GeneratesExpectedCalls()
    {
        const string source = CommonUsings + """
            namespace MyApp.Clients
            {
                [Serializer("xml")]
                public interface IDocumentApi
                {
                    [Multipart("----MyBoundary")]
                    [Post("/documents/{folder}")]
                    Task<HttpResult> UploadAsync(string folder, StreamPart file, [AliasAs("doc_title")] string title);
                }
            }
            """;

        var (sources, outputCompilation, _) = RunGenerator(source);
        var compilerErrors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(compilerErrors, Is.Empty, () => string.Join(Environment.NewLine, compilerErrors));
            Assert.That(sources, Has.Length.EqualTo(1));
        }

        var generatedCode = sources[0].SourceText.ToString();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(generatedCode, Does.Contain("_builder.AsMultipart(\"----MyBoundary\");"));
            Assert.That(generatedCode, Does.Contain("_builder.AddRouteParam(\"folder\", folder);"));
            Assert.That(generatedCode, Does.Contain("_builder.AddMultipartItem(\"file\", file, Serializer);"));
            Assert.That(generatedCode, Does.Contain("_builder.AddMultipartItem(\"doc_title\", title, Serializer);"));
        }
    }

    [Test]
    public void Generator_InheritedInterfaceAndMethodLevelInterceptors_GeneratesCompleteClientAndProxy()
    {
        const string source = CommonUsings + """
            namespace MyApp.Clients
            {
                public interface IBaseReadApi
                {
                    [Get("/items/{id}")]
                    Task<Result<UserDto>> GetItemResultAsync(int id);
                }

                [RestClient("/v2", "CustomItemsClient", "json")]
                public interface IItemsApi : IBaseReadApi
                {
                    [Intercept<TestInterceptor>]
                    [Post("/items")]
                    Task<Result> SaveItemAsync(UserDto item, [QueryParam("notify", "G")] int notifyFlag = 0);
                }
            }
            """;

        var (sources, outputCompilation, genDiagnostics) = RunGenerator(source);
        var compilerErrors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(genDiagnostics, Is.Empty);
            Assert.That(compilerErrors, Is.Empty, () => string.Join(Environment.NewLine, compilerErrors));
            Assert.That(sources, Has.Length.EqualTo(3));
            Assert.That(sources.Select(s => s.HintName), Does.Contain("BaseReadApiClient.g.cs"));
            Assert.That(sources.Select(s => s.HintName), Does.Contain("CustomItemsClient.g.cs"));
            Assert.That(sources.Select(s => s.HintName), Does.Contain("CustomItemsClientProxy.g.cs"));
        }
    }
}
