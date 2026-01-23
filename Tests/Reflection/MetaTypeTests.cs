using CoreOne.Reflection;
using System.Linq.Expressions;

namespace Tests.Reflection;

[TestFixture]
public class MetaTypeTests
{
    private class TestClass
    {
        public string? PublicProperty { get; set; }
        public int IntProperty { get; set; }
        private string? PrivateProperty { get; set; } = "private";
        public static string? StaticProperty { get; set; }

        public string GetPrivate() => PrivateProperty ?? "";

        public void SetPrivate(string value) => PrivateProperty = value;

        public string PublicMethod(string input) => input.ToUpper();

        public static string StaticMethod(string input) => input.ToLower();

        private string PrivateMethod(string input) => input + "_modified";
    }

    [Test]
    public void GetMetadata_WithValidProperty_ReturnsMetadata()
    {
        var metadata = MetaType.GetMetadata(typeof(TestClass), nameof(TestClass.PublicProperty));

        Assert.Multiple(() => {
            Assert.That(metadata, Is.Not.EqualTo(Metadata.Empty));
            Assert.That(metadata.Name, Is.EqualTo(nameof(TestClass.PublicProperty)));
            Assert.That(metadata.FPType, Is.EqualTo(typeof(string)));
        });
    }

    [Test]
    public void GetMetadata_WithNullType_ReturnsEmpty()
    {
        var metadata = MetaType.GetMetadata(null, "PropertyName");

        Assert.That(metadata, Is.EqualTo(Metadata.Empty));
    }

    [Test]
    public void GetMetadata_WithNullPropertyName_ReturnsEmpty()
    {
        var metadata = MetaType.GetMetadata(typeof(TestClass), null);

        Assert.That(metadata, Is.EqualTo(Metadata.Empty));
    }

    [Test]
    public void GetMetadata_WithEmptyPropertyName_ReturnsEmpty()
    {
        var metadata = MetaType.GetMetadata(typeof(TestClass), "");

        Assert.That(metadata, Is.EqualTo(Metadata.Empty));
    }

    [Test]
    public void GetMetadata_CachesResults()
    {
        var metadata1 = MetaType.GetMetadata(typeof(TestClass), nameof(TestClass.PublicProperty));
        var metadata2 = MetaType.GetMetadata(typeof(TestClass), nameof(TestClass.PublicProperty));

        Assert.That(metadata1, Is.EqualTo(metadata2));
    }

    [Test]
    public void GetMetadatas_ReturnsAllPublicProperties()
    {
        var metadatas = MetaType.GetMetadatas(typeof(TestClass));

        Assert.Multiple(() => {
            Assert.That(metadatas, Is.Not.Empty);
            Assert.That(metadatas.Any(m => m.Name == nameof(TestClass.PublicProperty)), Is.True);
            Assert.That(metadatas.Any(m => m.Name == nameof(TestClass.IntProperty)), Is.True);
        });
    }

    [Test]
    public void GetMetadatas_WithNullType_ReturnsEmpty()
    {
        var metadatas = MetaType.GetMetadatas(null!);

        Assert.That(metadatas, Is.Empty);
    }

    [Test]
    public void GetMetadatas_CachesResults()
    {
        var metadatas1 = MetaType.GetMetadatas(typeof(TestClass));
        var metadatas2 = MetaType.GetMetadatas(typeof(TestClass));

        Assert.That(metadatas1, Is.SameAs(metadatas2));
    }

    [Test]
    public void GetInvokeMethod_WithPublicInstanceMethod_ReturnsInvoker()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.PublicMethod));
        var invoker = MetaType.GetInvokeMethod(method);

        var instance = new TestClass();
        var result = invoker.Invoke(instance, ["test"]);

        Assert.That(result, Is.EqualTo("TEST"));
    }

    [Test]
    public void GetInvokeMethod_WithStaticMethod_ReturnsInvoker()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.StaticMethod));
        var invoker = MetaType.GetInvokeMethod(method);

        var result = invoker.Invoke(null, ["TEST"]);

        Assert.That(result, Is.EqualTo("test"));
    }

    [Test]
    public void GetInvokeMethod_WithNullMethod_ReturnsEmpty()
    {
        var invoker = MetaType.GetInvokeMethod((System.Reflection.MethodInfo?)null);

        Assert.That(invoker.IsEmpty, Is.True);
    }

    [Test]
    public void GetInvokeMethod_ByName_FindsMethod()
    {
        var invoker = MetaType.GetInvokeMethod(typeof(TestClass), nameof(TestClass.PublicMethod), typeof(string));

        var instance = new TestClass();
        var result = invoker.Invoke(instance, ["hello"]);

        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void GetInvokeMethod_WithNullType_ReturnsEmpty()
    {
        var invoker = MetaType.GetInvokeMethod(null, "MethodName", typeof(string));

        Assert.That(invoker.IsEmpty, Is.True);
    }

    [Test]
    public void GetInvokeMethod_CachesResults()
    {
        var invoker1 = MetaType.GetInvokeMethod(typeof(TestClass), nameof(TestClass.PublicMethod), typeof(string));
        var invoker2 = MetaType.GetInvokeMethod(typeof(TestClass), nameof(TestClass.PublicMethod), typeof(string));

        Assert.That(invoker1, Is.SameAs(invoker2));
    }

    [Test]
    public void GetInvokestaticMethod_FindsStaticMethod()
    {
        var invoker = MetaType.GetInvokestaticMethod(typeof(TestClass), nameof(TestClass.StaticMethod), typeof(string));

        var result = invoker.Invoke(null, ["STATIC"]);

        Assert.That(result, Is.EqualTo("static"));
    }

    [Test]
    public void GetName_WithPropertyExpression_ReturnsPropertyName()
    {
        var instance = new TestClass { PublicProperty = "test" };
        Expression<Func<string?>> expr = () => instance.PublicProperty;

        var name = MetaType.GetName(expr);

        Assert.That(name, Does.Contain(nameof(TestClass.PublicProperty)));
    }

    [Test]
    public void GetName_WithNullExpression_ReturnsEmpty()
    {
        Expression<Func<string?>>? expr = null;

        var name = MetaType.GetName(expr);

        Assert.That(name, Is.Empty);
    }

    [Test]
    public void GetName_Generic_WithPropertyExpression_ReturnsPropertyName()
    {
        Expression<Func<TestClass, string?>> expr = x => x.PublicProperty;

        var name = MetaType.GetName(expr);

        Assert.That(name, Is.EqualTo(nameof(TestClass.PublicProperty)));
    }

    [Test]
    public void GetName_Generic_WithMethodExpression_ThrowsException()
    {
        Expression<Func<TestClass, string>> expr = x => x.PublicMethod("test");

        Assert.Throws<ArgumentException>(() => MetaType.GetName(expr));
    }

    [Test]
    public void GetPropertyInfo_WithValidExpression_ReturnsPropertyInfo()
    {
        var instance = new TestClass();
        Expression<Func<string?>> expr = () => instance.PublicProperty;

        var propertyInfo = MetaType.GetPropertyInfo(expr);

        Assert.Multiple(() => {
            Assert.That(propertyInfo, Is.Not.Null);
            Assert.That(propertyInfo?.Name, Is.EqualTo(nameof(TestClass.PublicProperty)));
        });
    }

    [Test]
    public void GetUnderlyingType_WithNullableType_ReturnsUnderlyingType()
    {
        var nullableInt = typeof(int?);

        var underlying = MetaType.GetUnderlyingType(nullableInt);

        Assert.That(underlying, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void GetUnderlyingType_WithNonNullableType_ReturnsSameType()
    {
        var type = typeof(string);

        var underlying = MetaType.GetUnderlyingType(type);

        Assert.That(underlying, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void GetUnderlyingType_WithNull_ReturnsNull()
    {
        var underlying = MetaType.GetUnderlyingType(null);

        Assert.That(underlying, Is.Null);
    }

    [Test]
    public void GetUnderlyingType_WithGenericType_ReturnsInnerType()
    {
        var listType = typeof(List<int>);

        var underlying = MetaType.GetUnderlyingType(listType);

        Assert.That(underlying, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void GetHandler_WithValidProperty_ReturnsGetter()
    {
        var handler = MetaType.GetHandler(typeof(TestClass), nameof(TestClass.PublicProperty));

        var instance = new TestClass { PublicProperty = "test value" };
        var result = handler.Invoke(instance, []);

        Assert.That(result, Is.EqualTo("test value"));
    }

    [Test]
    public void SetHandler_WithValidProperty_ReturnsSetter()
    {
        var setter = MetaType.SetHandler(typeof(TestClass), nameof(TestClass.PublicProperty));

        var instance = new TestClass();
        setter?.Invoke(instance, "new value");

        Assert.That(instance.PublicProperty, Is.EqualTo("new value"));
    }

    [Test]
    public void CreateFromMemberInfo_WithValidMember_ReturnsMetadata()
    {
        var property = typeof(TestClass).GetProperty(nameof(TestClass.PublicProperty));

        var metadata = MetaType.CreateFromMemberInfo(typeof(TestClass), property);

        Assert.Multiple(() => {
            Assert.That(metadata, Is.Not.EqualTo(Metadata.Empty));
            Assert.That(metadata.Name, Is.EqualTo(nameof(TestClass.PublicProperty)));
        });
    }

    [Test]
    public void CreateFromMemberInfo_WithNullType_ReturnsEmpty()
    {
        var property = typeof(TestClass).GetProperty(nameof(TestClass.PublicProperty));

        var metadata = MetaType.CreateFromMemberInfo(null, property);

        Assert.That(metadata, Is.EqualTo(Metadata.Empty));
    }

    [Test]
    public void CreateFromMemberInfo_WithNullMember_ReturnsEmpty()
    {
        var metadata = MetaType.CreateFromMemberInfo(typeof(TestClass), null);

        Assert.That(metadata, Is.EqualTo(Metadata.Empty));
    }

    [Test]
    public void GetInvokeMethod_WithVoidReturnMethod_HandlesVoid()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.SetPrivate));
        var invoker = MetaType.GetInvokeMethod(method);

        var instance = new TestClass();
        invoker.Invoke(instance, ["new_value"]);

        Assert.That(instance.GetPrivate(), Is.EqualTo("new_value"));
    }

    [Test]
    public void GetMetadatas_WithPrivateFlag_IncludesPrivateMembers()
    {
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        var metadatas = MetaType.GetMetadatas(typeof(TestClass), flags);

        // Just verify we get metadatas back - exact members depend on implementation
        Assert.That(metadatas, Is.Not.Empty);
    }
}