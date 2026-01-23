using CoreOne.Extensions;
using NUnit.Framework;

namespace Tests.Extensions;

public class DictionaryExtensionsTests
{
    [Test]
    public void GetSet_AddsValueWhenKeyNotExists()
    {
        var dict = new Dictionary<string, int>();
        var result = dict.GetSet("key", () => 42);
        Assert.That(result, Is.EqualTo(42));
        Assert.That(dict["key"], Is.EqualTo(42));
    }

    [Test]
    public void GetSet_ReturnsExistingValue_WhenKeyExists()
    {
        var dict = new Dictionary<string, int> { { "key", 100 } };
        var getterInvoked = false;
        var result = dict.GetSet("key", () => {
            getterInvoked = true;
            return 42;
        });
        Assert.That(result, Is.EqualTo(100));
        Assert.That(getterInvoked, Is.False);
    }

    [Test]
    public void GetValue_ReturnsValue_WhenKeyExists()
    {
        var dict = new Dictionary<string, int> { { "key", 42 } };
        var result = dict.GetValue("key");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void GetValue_ReturnsDefault_WhenKeyNotExists()
    {
        var dict = new Dictionary<string, string>();
        var result = dict.GetValue("key");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetValue_ReturnsDefaultValue_WhenKeyNotExists()
    {
        var dict = new Dictionary<string, int>();
        var result = dict.GetValue("key", () => 999);
        Assert.That(result, Is.EqualTo(999));
    }

    [Test]
    public void GetValue_HandlesNullKey_WithDefaultValue()
    {
        var dict = new Dictionary<string, int>();
        var result = dict.GetValue(null, () => 100);
        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void GetValue_HandlesNullDictionary()
    {
        Dictionary<string, int>? dict = null;
        var result = dict.GetValue("key", () => 50);
        Assert.That(result, Is.EqualTo(50));
    }
}
