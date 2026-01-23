using CoreOne.Collections;
using NUnit.Framework;

namespace Tests.Collections;

public class DataTests
{
    [Test]
    public void Constructor_CreatesEmptyDictionary()
    {
        var data = new Data<string, int>();
        
        Assert.That(data.Count, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_WithSize_CreatesEmptyDictionary()
    {
        var data = new Data<string, int>(10);
        
        Assert.That(data.Count, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_WithComparer_UsesComparer()
    {
        var data = new Data<string, int>(StringComparer.OrdinalIgnoreCase);
        data["Key"] = 1;
        
        Assert.That(data["KEY"], Is.EqualTo(1));
    }

    [Test]
    public void Set_AddsNewValue()
    {
        var data = new Data<string, int>();
        data.Set("key", 42);
        
        Assert.That(data["key"], Is.EqualTo(42));
    }

    [Test]
    public void Set_UpdatesExistingValue()
    {
        var data = new Data<string, int>();
        data.Set("key", 42);
        data.Set("key", 100);
        
        Assert.That(data["key"], Is.EqualTo(100));
    }

    [Test]
    public void Get_ReturnsValue_WhenKeyExists()
    {
        var data = new Data<string, int>();
        data.Set("key", 42);
        
        var value = data.Get("key");
        
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void Get_ReturnsDefault_WhenKeyNotExists()
    {
        var data = new Data<string, int>();
        
        var value = data.Get("nonexistent");
        
        Assert.That(value, Is.EqualTo(0));
    }

    [Test]
    public void Get_ReturnsDefaultKey_WhenKeyNotExistsAndDefaultKeySet()
    {
        var data = new Data<string, int> { DefaultKey = "default" };
        data.Set("default", 999);
        
        var value = data.Get("nonexistent");
        
        Assert.That(value, Is.EqualTo(999));
    }

    [Test]
    public void Indexer_SetsValue()
    {
        var data = new Data<string, int> { ["key"] = 42 };
        
        Assert.That(data["key"], Is.EqualTo(42));
    }

    [Test]
    public void Indexer_GetsValue()
    {
        var data = new Data<string, int>();
        data.Set("key", 42);
        
        var value = data["key"];
        
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void Indexer_ReturnsDefault_WhenKeyNotExists()
    {
        var data = new Data<string, string>();
        
        var value = data["nonexistent"];
        
        Assert.That(value, Is.Null);
    }

    [Test]
    public void DefaultKey_CanBeSet()
    {
        var data = new Data<string, int> { DefaultKey = "default" };
        
        Assert.That(data.DefaultKey, Is.EqualTo("default"));
    }

    [Test]
    public void Clear_RemovesAllItems()
    {
        var data = new Data<string, int> { ["key1"] = 1, ["key2"] = 2 };
        
        data.Clear();
        
        Assert.That(data.Count, Is.EqualTo(0));
    }

    [Test]
    public void ContainsKey_ReturnsTrueWhenKeyExists()
    {
        var data = new Data<string, int> { ["key"] = 42 };
        
        Assert.That(data.ContainsKey("key"), Is.True);
    }

    [Test]
    public void ContainsKey_ReturnsFalseWhenKeyNotExists()
    {
        var data = new Data<string, int>();
        
        Assert.That(data.ContainsKey("key"), Is.False);
    }

    [Test]
    public void Remove_RemovesKey()
    {
        var data = new Data<string, int> { ["key"] = 42 };
        
        data.Remove("key");
        
        Assert.That(data.ContainsKey("key"), Is.False);
    }

    [Test]
    public void TryGetValue_ReturnsTrue_WhenKeyExists()
    {
        var data = new Data<string, int> { ["key"] = 42 };
        
        var success = data.TryGetValue("key", out var value);
        
        Assert.That(success, Is.True);
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void TryGetValue_ReturnsFalse_WhenKeyNotExists()
    {
        var data = new Data<string, int>();
        
        var success = data.TryGetValue("key", out var value);
        
        Assert.That(success, Is.False);
        Assert.That(value, Is.EqualTo(0));
    }

    [Test]
    public void Keys_ReturnsAllKeys()
    {
        var data = new Data<string, int> { ["key1"] = 1, ["key2"] = 2 };
        
        var keys = data.Keys.ToList();
        
        Assert.That(keys.Count, Is.EqualTo(2));
        Assert.That(keys, Does.Contain("key1"));
        Assert.That(keys, Does.Contain("key2"));
    }

    [Test]
    public void Values_ReturnsAllValues()
    {
        var data = new Data<string, int> { ["key1"] = 1, ["key2"] = 2 };
        
        var values = data.Values.ToList();
        
        Assert.That(values.Count, Is.EqualTo(2));
        Assert.That(values, Does.Contain(1));
        Assert.That(values, Does.Contain(2));
    }
}
