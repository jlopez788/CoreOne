using CoreOne;
using NUnit.Framework;

namespace Tests;

public class UtilityHashTests
{
    [Test]
    public void Crc32_String_GeneratesHash()
    {
        var result = Utility.Crc32("test data");
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Model, Is.Not.Null);
        Assert.That(result.Model, Is.Not.Empty);
    }

    [Test]
    public void Crc32_SameInput_GeneratesSameHash()
    {
        var result1 = Utility.Crc32("test data");
        var result2 = Utility.Crc32("test data");
        
        Assert.That(result1.Model, Is.EqualTo(result2.Model));
    }

    [Test]
    public void Crc32_DifferentInput_GeneratesDifferentHash()
    {
        var result1 = Utility.Crc32("test data 1");
        var result2 = Utility.Crc32("test data 2");
        
        Assert.That(result1.Model, Is.Not.EqualTo(result2.Model));
    }

    [Test]
    public void Crc32AsInt_GeneratesIntegerHash()
    {
        var result = Utility.Crc32AsInt("test data");
        
        Assert.That(result, Is.Not.Zero);
    }

    [Test]
    public void Crc32AsInt_SameInput_GeneratesSameHash()
    {
        var result1 = Utility.Crc32AsInt("test data");
        var result2 = Utility.Crc32AsInt("test data");
        
        Assert.That(result1, Is.EqualTo(result2));
    }

    [Test]
    public void HashMD5_String_GeneratesHash()
    {
        var result = Utility.HashMD5("test data");
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Model, Is.Not.Null);
        Assert.That(result.Model!.Length, Is.EqualTo(32)); // MD5 hash is 32 hex chars
    }

    [Test]
    public void HashMD5_SameInput_GeneratesSameHash()
    {
        var result1 = Utility.HashMD5("test data");
        var result2 = Utility.HashMD5("test data");
        
        Assert.That(result1.Model, Is.EqualTo(result2.Model));
    }

    [Test]
    public void HashSHA1_String_GeneratesHash()
    {
        var result = Utility.HashSHA1("test data");
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Model, Is.Not.Null);
        Assert.That(result.Model!.Length, Is.EqualTo(40)); // SHA1 hash is 40 hex chars
    }

    [Test]
    public void HashSHA1_SameInput_GeneratesSameHash()
    {
        var result1 = Utility.HashSHA1("test data");
        var result2 = Utility.HashSHA1("test data");
        
        Assert.That(result1.Model, Is.EqualTo(result2.Model));
    }

    [Test]
    public void HashSHA256_String_GeneratesHash()
    {
        var result = Utility.HashSHA256("test data");
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Model, Is.Not.Null);
        Assert.That(result.Model!.Length, Is.EqualTo(64)); // SHA256 hash is 64 hex chars
    }

    [Test]
    public void HashSHA256_SameInput_GeneratesSameHash()
    {
        var result1 = Utility.HashSHA256("test data");
        var result2 = Utility.HashSHA256("test data");
        
        Assert.That(result1.Model, Is.EqualTo(result2.Model));
    }

    [Test]
    public void HashSHA256_DifferentInput_GeneratesDifferentHash()
    {
        var result1 = Utility.HashSHA256("test data 1");
        var result2 = Utility.HashSHA256("test data 2");
        
        Assert.That(result1.Model, Is.Not.EqualTo(result2.Model));
    }

    [Test]
    public void ToBase64_String_EncodesCorrectly()
    {
        var result = Utility.ToBase64("test data");
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Model, Is.Not.Null);
        Assert.That(result.Model, Is.Not.Empty);
    }

    [Test]
    public void ToBase64_FromBase64_RoundTrip()
    {
        var original = "test data";
        var encoded = Utility.ToBase64(original);
        var decoded = Utility.FromBase64(encoded.Model);
        var decodedString = System.Text.Encoding.ASCII.GetString(decoded.Model!);
        
        Assert.That(decodedString, Is.EqualTo(original));
    }

    [Test]
    public void FromBase64_ValidBase64_DecodesCorrectly()
    {
        var base64 = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("test data"));
        var result = Utility.FromBase64(base64);
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Model, Is.Not.Null);
        var decoded = System.Text.Encoding.ASCII.GetString(result.Model!);
        Assert.That(decoded, Is.EqualTo("test data"));
    }

    [Test]
    public void FromBase64_NullInput_ReturnsEmpty()
    {
        var result = Utility.FromBase64(null);
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Model, Is.Empty);
    }

    [Test]
    public void FromBase64_InvalidBase64_ReturnsFailure()
    {
        var result = Utility.FromBase64("invalid-base64!!!");
        
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void HashMD5_WithCustomKey_GeneratesHash()
    {
        var key = System.Text.Encoding.ASCII.GetBytes("custom-key-12345");
        var result = Utility.HashMD5("test data", key);
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Model, Is.Not.Null);
    }

    [Test]
    public void HashMD5_DifferentKeys_GenerateDifferentHashes()
    {
        var key1 = System.Text.Encoding.ASCII.GetBytes("key1");
        var key2 = System.Text.Encoding.ASCII.GetBytes("key2");
        
        var result1 = Utility.HashMD5("test data", key1);
        var result2 = Utility.HashMD5("test data", key2);
        
        Assert.That(result1.Model, Is.Not.EqualTo(result2.Model));
    }
}
