using CoreOne.Extensions;

namespace Tests.Extensions;

public class ObjectExtensionsTests
{
    private class TestModel
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    [Test]
    public void IsNull_NullObject_ReturnsTrue()
    {
        object? obj = null;
        
        Assert.That(obj.IsNull(), Is.True);
    }

    [Test]
    public void IsNull_DBNull_ReturnsTrue()
    {
        object obj = DBNull.Value;
        
        Assert.That(obj.IsNull(), Is.True);
    }

    [Test]
    public void IsNull_NonNullObject_ReturnsFalse()
    {
        object obj = new();
        
        Assert.That(obj.IsNull(), Is.False);
    }

    [Test]
    public void IsNotNull_NullObject_ReturnsFalse()
    {
        object? obj = null;
        
        Assert.That(obj.IsNotNull(), Is.False);
    }

    [Test]
    public void IsNotNull_NonNullObject_ReturnsTrue()
    {
        object obj = new();
        
        Assert.That(obj.IsNotNull(), Is.True);
    }

    [Test]
    public void IsNotNull_DBNull_ReturnsFalse()
    {
        object obj = DBNull.Value;
        
        Assert.That(obj.IsNotNull(), Is.False);
    }

    [Test]
    public void ToODictionary_NullObject_ReturnsEmptyDictionary()
    {
        object? obj = null;
        
        var result = obj.ToODictionary();
        
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ToODictionary_ValidObject_ReturnsDictionary()
    {
        var obj = new TestModel { Name = "John", Age = 30, IsActive = true };
        
        var result = obj.ToODictionary();
        
        Assert.That(result, Is.Not.Empty);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ContainsKey("name"), Is.True);
            Assert.That(result.ContainsKey("age"), Is.True);
            Assert.That(result.ContainsKey("is-active"), Is.True);
        }
    }

    [Test]
    public void ToODictionary_CustomSeparator_UsesCustomSeparator()
    {
        var obj = new TestModel { Name = "John", Age = 30, IsActive = true };
        
        var result = obj.ToODictionary("_");
        
        Assert.That(result.ContainsKey("is_active"), Is.True);
    }

    [Test]
    public void ToODictionary_PropertyValues_AreCorrect()
    {
        var obj = new TestModel { Name = "John", Age = 30, IsActive = true };
        
        var result = obj.ToODictionary();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result["name"], Is.EqualTo("John"));
            Assert.That(result["age"], Is.EqualTo(30));
            Assert.That(result["is-active"], Is.True);
        }
    }

    [Test]
    public void ToODictionary_NullProperties_AreExcluded()
    {
        var obj = new TestModel { Name = null!, Age = 0, IsActive = false };
        
        var result = obj.ToODictionary();
        
        Assert.That(result.ContainsKey("name"), Is.False);
    }

    [Test]
    public void IsNull_String_ReturnsFalse()
    {
        object obj = "test";
        
        Assert.That(obj.IsNull(), Is.False);
    }

    [Test]
    public void IsNotNull_EmptyString_ReturnsTrue()
    {
        object obj = "";
        
        Assert.That(obj.IsNotNull(), Is.True);
    }
}
