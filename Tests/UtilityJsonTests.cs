using CoreOne;
using CoreOne.Reflection;
using NUnit.Framework;
using System.Text;

namespace Tests;

public class UtilityJsonTests
{
    public class TestModel
    {
        public DateTime Created { get; set; }
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class TypekeyModel
    {
        public TypeKey Key { get; set; }
    }

    [Test]
    public void Deserialize_NonGeneric_DeserializesType()
    {
        var json = "{\"Id\":1,\"Name\":\"Test\"}";
        var obj = Utility.Deserialize(typeof(TestModel), json);

        Assert.That(obj, Is.Not.Null);
        Assert.That(obj, Is.InstanceOf<TestModel>());
        var model = (TestModel)obj!;
        Assert.That(model.Id, Is.EqualTo(1));
    }

    [Test]
    public void DeserializeObject_DeserializesJson()
    {
        var json = "{\"Id\":1,\"Name\":\"Test\"}";
        var model = Utility.DeserializeObject<TestModel>(json);

        Assert.That(model, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(model!.Id, Is.EqualTo(1));
            Assert.That(model.Name, Is.EqualTo("Test"));
        }
    }

    [Test]
    public void DeserializeObject_EmptyJson_ReturnsDefault()
    {
        var model = Utility.DeserializeObject<TestModel>("");

        Assert.That(model, Is.Null);
    }

    [Test]
    public void DeserializeObject_FromStream_ReadsCorrectly()
    {
        var json = "{\"Id\":1,\"Name\":\"Test\"}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var result = Utility.DeserializeObject<TestModel>(stream);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.Not.Null);
            Assert.That(result.Model!.Id, Is.EqualTo(1));
            Assert.That(result.Model.Name, Is.EqualTo("Test"));
        }
    }

    [Test]
    public void DeserializeObject_NullJson_ReturnsDefault()
    {
        var model = Utility.DeserializeObject<TestModel>((string?)null);

        Assert.That(model, Is.Null);
    }

    [Test]
    public void Serialize_NullObject_ReturnsEmpty()
    {
        TestModel? model = null;
        var json = Utility.Serialize(model);

        Assert.That(json, Is.Empty);
    }

    [Test]
    public void Serialize_SerializesObject()
    {
        var model = new TestModel { Id = 1, Name = "Test", Created = new DateTime(2026, 1, 1) };
        var json = Utility.Serialize(model);

        Assert.That(json, Is.Not.Null);
        Assert.That(json, Does.Contain("\"id\":1").Or.Contain("\"Id\":1"));
        Assert.That(json, Does.Contain("\"name\":\"Test\"").Or.Contain("\"Name\":\"Test\""));
    }

    [Test]
    public void Serialize_TypeKey()
    {
        var name = "Test";
        var key = TypeKeyStore.Register<string>(name);
        var json = Utility.Serialize(new TypekeyModel { Key = key });

        Assert.That(json, Is.EqualTo("{\"key\":\"Test\"}"));
        var model = Utility.DeserializeObject<TypekeyModel>(json);
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Key, Is.EqualTo(key));
    }

    [Test]
    public void Serialize_WithPrettyPrint_FormatsJson()
    {
        var model = new TestModel { Id = 1, Name = "Test" };
        var json = Utility.Serialize(model, prettyPrint: true);

        Assert.That(json, Does.Contain("\n"));
        Assert.That(json, Does.Contain("  "));
    }

    [Test]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        var original = new TestModel { Id = 42, Name = "RoundTrip", Created = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc) };
        var json = Utility.Serialize(original);
        var deserialized = Utility.DeserializeObject<TestModel>(json);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(deserialized!.Id, Is.EqualTo(original.Id));
            Assert.That(deserialized.Name, Is.EqualTo(original.Name));
            Assert.That(deserialized.Created, Is.EqualTo(original.Created).Within(TimeSpan.FromSeconds(1)));
        }
    }

    [Test]
    public void SerializeToStream_WithPrettyPrint_FormatsJson()
    {
        var model = new TestModel { Id = 1, Name = "Test" };
        using var stream = new MemoryStream();

        var result = Utility.SerializeToStream(model, stream, prettyPrint: true);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void SerializeToStream_WritesToStream()
    {
        var model = new TestModel { Id = 1, Name = "Test" };
        using var stream = new MemoryStream();

        var result = Utility.SerializeToStream(model, stream);

        Assert.That(result.Success, Is.True);
    }

    [SetUp]
    public void Setup()
    {
        Utility.InitializeSettings();
    }

    [Test]
    public async Task ToStringContent_ContainsSerializedData()
    {
        var model = new TestModel { Id = 1, Name = "Test" };
        var content = model.ToStringContent();
        var json = await content!.ReadAsStringAsync();

        Assert.That(json, Does.Contain("\"id\":1").Or.Contain("\"Id\":1"));
        Assert.That(json, Does.Contain("\"name\":\"Test\"").Or.Contain("\"Name\":\"Test\""));
    }

    [Test]
    public void ToStringContent_CreatesHttpContent()
    {
        var model = new TestModel { Id = 1, Name = "Test" };
        var content = model.ToStringContent();

        Assert.That(content, Is.Not.Null);
        Assert.That(content!.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
    }

    [Test]
    public void ToStringContent_NullModel_ReturnsNull()
    {
        TestModel? model = null;
        var content = model.ToStringContent();

        Assert.That(content, Is.Null);
    }
}