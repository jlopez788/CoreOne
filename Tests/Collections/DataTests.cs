using CoreOne.Collections;
using NUnit.Framework;

namespace Tests.Collections;

public class DataTests
{
    [Test]
    public void Constructor_CreatesEmptyDictionary()
    {
        var data = new Data<string, int>();
        
        Assert.That(data, Is.Empty);
    }

    [Test]
    public void Constructor_WithSize_CreatesEmptyDictionary()
    {
        var data = new Data<string, int>(10);
        
        Assert.That(data, Is.Empty);
    }

    [Test]
    public void Constructor_WithComparer_UsesComparer()
    {
        var data = new Data<string, int>(StringComparer.OrdinalIgnoreCase) {
            ["Key"] = 1
        };
        
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
        
        Assert.That(data, Is.Empty);
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.True);
            Assert.That(value, Is.EqualTo(42));
        }
    }

    [Test]
    public void TryGetValue_ReturnsFalse_WhenKeyNotExists()
    {
        var data = new Data<string, int>();
        
        var success = data.TryGetValue("key", out var value);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(success, Is.False);
            Assert.That(value, Is.EqualTo(0));
        }
    }

    [Test]
    public void Keys_ReturnsAllKeys()
    {
        var data = new Data<string, int> { ["key1"] = 1, ["key2"] = 2 };
        
        var keys = data.Keys.ToList();
        
        Assert.That(keys, Has.Count.EqualTo(2));
        Assert.That(keys, Does.Contain("key1"));
        Assert.That(keys, Does.Contain("key2"));
    }

    [Test]
    public void Values_ReturnsAllValues()
    {
        var data = new Data<string, int> { ["key1"] = 1, ["key2"] = 2 };
        
        var values = data.Values.ToList();
        
        Assert.That(values, Has.Count.EqualTo(2));
        Assert.That(values, Does.Contain(1));
        Assert.That(values, Does.Contain(2));
    }

    [Test]
    public void Get_WithGetter_ReturnsGetterValue()
    {
        var data = new Data<string, int>();
        
        var value = data.Get("nonexistent", () => 999);
        
        Assert.That(value, Is.EqualTo(999));
    }

    [Test]
    public void RemoveAll_WithPredicate_RemovesMatchingItems()
    {
        var data = new Data<string, int> { ["key1"] = 1, ["key2"] = 2, ["key3"] = 3 };
        
        data.RemoveAll((k, v) => v > 1);
        
        Assert.That(data, Has.Count.EqualTo(1));
        Assert.That(data["key1"], Is.EqualTo(1));
    }

    [Test]
    public void SafeAdd_AddsNewKey()
    {
        var data = new Data<string, int>();
        
        data.SafeAdd("key", 42);
        
        Assert.That(data["key"], Is.EqualTo(42));
    }

    [Test]
    public void SafeAdd_IgnoresExistingKey()
    {
        var data = new Data<string, int> { ["key"] = 42 };
        
        data.SafeAdd("key", 100);
        
        Assert.That(data["key"], Is.EqualTo(42));
    }

    [Test]
    public void SafeAdd_NullKey_DoesNothing()
    {
        var data = new Data<string, int>();
        
        data.SafeAdd(null, 42);
        
        Assert.That(data, Is.Empty);
    }

    [Test]
    public void SafeAddRange_AddsMultipleItems()
    {
        var data = new Data<string, int>();
        var items = new[] { 1, 2, 3 };
        
        data.SafeAddRange(items, x => $"key{x}");
        
        Assert.That(data, Has.Count.EqualTo(3));
    }

    [Test]
    public void SafeAddRange_IgnoresExistingKeys()
    {
        var data = new Data<string, int> { ["key1"] = 100 };
        var items = new[] { 1, 2, 3 };
        
        data.SafeAddRange(items, x => $"key{x}");
        
        Assert.That(data["key1"], Is.EqualTo(100));
    }

    [Test]
    public void SafeAddRange_NullItems_DoesNothing()
    {
        var data = new Data<string, int>();
        
        data.SafeAddRange(null, x => "key");
        
        Assert.That(data, Is.Empty);
    }

    [Test]
    public void SetDefaultKey_SetsDefaultKey()
    {
        var data = new Data<string, int>();
        
        data.SetDefaultKey("default").Set("default", 999);
        var value = data.Get("nonexistent");
        
        Assert.That(value, Is.EqualTo(999));
    }

    [Test]
    public void SetRange_AddsMultipleItems()
    {
        var data = new Data<string, int>();
        var items = new[] { 1, 2, 3 };
        
        data.SetRange(items, x => $"key{x}");
        
        Assert.That(data, Has.Count.EqualTo(3));
    }

    [Test]
    public void SetRange_OverwritesExisting()
    {
        var data = new Data<string, int> { ["key1"] = 100 };
        var items = new[] { 1, 2, 3 };
        
        data.SetRange(items, x => $"key{x}");
        
        Assert.That(data["key1"], Is.EqualTo(1));
    }

    [Test]
    public void SetRange_NullItems_DoesNothing()
    {
        var data = new Data<string, int>();
        
        data.SetRange(null, x => "key");
        
        Assert.That(data, Is.Empty);
    }

    [Test]
    public void Set_NullKey_DoesNotAdd()
    {
        var data = new Data<string, int>();
        
        data.Set(null, 42);
        
        Assert.That(data, Is.Empty);
    }

    [Test]
    public void Set_NullValue_DoesNotAdd()
    {
        var data = new Data<string, string?>();
        
        data.Set("key", null);
        
        Assert.That(data, Is.Empty);
    }

    [Test]
    public void Indexer_NullValue_DoesNotAdd()
    {
        var data = new Data<string, string?> { ["key"] = null };
        
        Assert.That(data, Is.Empty);
    }

    [Test]
    public void Constructor_WithDictionary_CopiesValues()
    {
        var source = new Dictionary<string, int> { ["key1"] = 1, ["key2"] = 2 };
        
        var data = new Data<string, int>(source);
        
        Assert.That(data, Has.Count.EqualTo(2));
        Assert.That(data["key1"], Is.EqualTo(1));
    }

    [Test]
    public void Constructor_WithDictionaryAndComparer_CopiesValuesAndUsesComparer()
    {
        var source = new Dictionary<string, int> { ["key1"] = 1, ["key2"] = 2 };
        
        var data = new Data<string, int>(source, StringComparer.OrdinalIgnoreCase);
        
        Assert.That(data["KEY1"], Is.EqualTo(1));
    }
}
