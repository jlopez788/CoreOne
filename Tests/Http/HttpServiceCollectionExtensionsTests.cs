using CoreOne;
using CoreOne.Extensions;
using CoreOne.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;

namespace Tests.Http;

public interface INotGeneratedApi
{
    Task DoWorkAsync();
}

public class HttpServiceCollectionExtensionsTests
{
    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }

    [Test]
    public void AddRestClient_NonInterfaceType_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddRestClient<CustomerDto>());
    }

    [Test]
    public void AddRestClient_InterfaceWithoutGeneratedClient_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddRestClient<INotGeneratedApi>());
    }

    [Test]
    public void AddRestClient_ExplicitClientType_RegistersConcreteClient()
    {
        var services = new ServiceCollection();
        services.AddCoreServices();
        services.AddRestClient<ICustomerApi, CustomerApiClient>((_, client) => client.BaseAddress = new Uri("https://example.test"));

        using var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<ICustomerApi>();

        Assert.That(api, Is.InstanceOf<CustomerApiClient>());
    }

    [Test]
    public async Task AddRestClient_WithSerializerKey_UsesKeyedSerializerForResponses()
    {
        var xmlSerializer = new XmlTestSerializer();
        var xmlPayload = Encoding.UTF8.GetString(xmlSerializer.Serialize(new CustomerDto { Id = 5, Name = "Xml Customer" }));

        var services = new ServiceCollection();
        services.AddCoreServices();
        services.AddKeyedSingleton<ISerializer, XmlTestSerializer>("xml");
        services.AddRestClient<ICustomerApi>(client => client.BaseAddress = new Uri("https://example.test"), serializerKey: "xml")
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(xmlPayload, Encoding.UTF8, "application/xml")
                })));

        using var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<ICustomerApi>();

        var result = await api.GetCustomerAsync(5);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model?.Id, Is.EqualTo(5));
            Assert.That(result.Model?.Name, Is.EqualTo("Xml Customer"));
        }
    }

    [Test]
    public void AddRestClient_RegisteredTwice_LastRegistrationWins()
    {
        var services = new ServiceCollection();
        services.AddCoreServices();
        services.AddRestClient<ICustomerApi>(client => client.BaseAddress = new Uri("https://first.test"));
        services.AddRestClient<ICustomerApi>(client => client.BaseAddress = new Uri("https://second.test"));

        using var sp = services.BuildServiceProvider();
        var api = sp.GetRequiredService<ICustomerApi>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(api, Is.Not.Null);
            Assert.That(services.Count(d => d.ServiceType == typeof(ICustomerApi)), Is.EqualTo(1));
        }
    }
}
