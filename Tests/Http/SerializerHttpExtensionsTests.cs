using CoreOne;
using CoreOne.Http.Extensions;
using CoreOne.Results;
using CoreOne.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Tests.Http;

public class SerializerHttpExtensionsTests
{
    public class StubSerializer : ISerializer
    {
        public string ContentType { get; set; } = "application/x-stub";
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public IResult<object> Deserialize(byte[]? data, Type objectType) => new Result<object>(null);

        public Task<IResult<object>> DeserializeAsync(Stream stream, Type objectType, CancellationToken cancellationToken = default) =>
            Task.FromResult<IResult<object>>(new Result<object>(null));

        public byte[] Serialize(object? model, Type? objectType = null) => Encoding.GetBytes("stub");
    }

    public class OtherStubSerializer : StubSerializer;

    [Test]
    public void ResolveSerializer_NullServices_ReturnsBuiltInJsonInstance()
    {
        IServiceProvider? services = null;

        var serializer = services.ResolveSerializer();

        Assert.That(serializer, Is.SameAs(NJsonService.Instance));
    }

    [Test]
    public void ResolveSerializer_SerializerTypeRegistered_ReturnsRegisteredInstance()
    {
        var instance = new StubSerializer();
        using var sp = new ServiceCollection()
            .AddSingleton(instance)
            .BuildServiceProvider();

        var serializer = sp.ResolveSerializer(serializerType: typeof(StubSerializer));

        Assert.That(serializer, Is.SameAs(instance));
    }

    [Test]
    public void ResolveSerializer_SerializerTypeWithNullServices_CreatesViaActivator()
    {
        IServiceProvider? services = null;

        var serializer = services.ResolveSerializer(serializerType: typeof(StubSerializer));

        Assert.That(serializer, Is.InstanceOf<StubSerializer>());
    }

    [Test]
    public void ResolveSerializer_SerializerTypeTakesPrecedenceOverKey()
    {
        using var sp = new ServiceCollection()
            .AddKeyedSingleton<ISerializer, OtherStubSerializer>("stub")
            .AddSingleton(new StubSerializer())
            .BuildServiceProvider();

        var serializer = sp.ResolveSerializer("stub", typeof(StubSerializer));

        Assert.That(serializer, Is.InstanceOf<StubSerializer>());
        Assert.That(serializer, Is.Not.InstanceOf<OtherStubSerializer>());
    }

    [Test]
    public void ResolveSerializer_KeyedServiceRegistered_ReturnsKeyedService()
    {
        using var sp = new ServiceCollection()
            .AddKeyedSingleton<ISerializer, StubSerializer>("stub")
            .BuildServiceProvider();

        var serializer = sp.ResolveSerializer("stub");

        Assert.That(serializer, Is.InstanceOf<StubSerializer>());
    }

    [Test]
    public void ResolveSerializer_KeyNotRegistered_UsesFallback()
    {
        var fallback = new StubSerializer();
        using var sp = new ServiceCollection().BuildServiceProvider();

        var serializer = sp.ResolveSerializer("missing-key", fallback: fallback);

        Assert.That(serializer, Is.SameAs(fallback));
    }

    [Test]
    public void ResolveSerializer_DefaultServiceRegistered_ReturnsDefaultService()
    {
        var instance = new StubSerializer();
        using var sp = new ServiceCollection()
            .AddSingleton<ISerializer>(instance)
            .BuildServiceProvider();

        var serializer = sp.ResolveSerializer();

        Assert.That(serializer, Is.SameAs(instance));
    }

    [Test]
    public void ResolveSerializer_OnlyKeyedJsonRegistered_UsedAsDefault()
    {
        var instance = new StubSerializer();
        using var sp = new ServiceCollection()
            .AddKeyedSingleton<ISerializer>("json", instance)
            .BuildServiceProvider();

        var serializer = sp.ResolveSerializer();

        Assert.That(serializer, Is.SameAs(instance));
    }

    [Test]
    public void ResolveSerializer_NothingRegistered_ReturnsBuiltInJsonInstance()
    {
        using var sp = new ServiceCollection().BuildServiceProvider();

        var serializer = sp.ResolveSerializer();

        Assert.That(serializer, Is.SameAs(NJsonService.Instance));
    }

    [Test]
    public void ToHttpContent_NullModel_ReturnsNull()
    {
        var content = NJsonService.Instance.ToHttpContent(null);

        Assert.That(content, Is.Null);
    }

    [Test]
    public void ToHttpContent_HttpContentModel_ReturnsSameInstance()
    {
        using var existing = new StringContent("raw");

        var content = NJsonService.Instance.ToHttpContent(existing);

        Assert.That(content, Is.SameAs(existing));
    }

    [Test]
    public async Task ToHttpContent_Model_SerializesWithContentTypeAndCharset()
    {
        var serializer = new StubSerializer();

        using var content = serializer.ToHttpContent(new { Value = 1 });
        var body = await content!.ReadAsStringAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(content.Headers.ContentType?.MediaType, Is.EqualTo("application/x-stub"));
            Assert.That(content.Headers.ContentType?.CharSet, Is.EqualTo("utf-8"));
            Assert.That(body, Is.EqualTo("stub"));
        }
    }

    [Test]
    public void ToHttpContent_EmptyContentType_DefaultsToApplicationJson()
    {
        var serializer = new StubSerializer { ContentType = string.Empty };

        using var content = serializer.ToHttpContent(new { Value = 1 });

        Assert.That(content!.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
    }
}
