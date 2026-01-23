using CoreOne.Extensions;
using NUnit.Framework;

namespace Tests.Extensions;

public class IDExtensionsTests
{
    [Test]
    public void ToShortId_CreatesShortIdentifier()
    {
        var guid = Guid.NewGuid();
        var shortId = guid.ToShortId();
        
        Assert.That(shortId, Is.Not.Null);
        Assert.That(shortId.Length, Is.LessThan(36)); // Shorter than full GUID
        Assert.That(shortId, Does.Not.Contain("+"));
        Assert.That(shortId, Does.Not.Contain("/"));
        Assert.That(shortId, Does.Not.Contain("="));
    }

    [Test]
    public void ToShortId_SameGuid_ProducesSameResult()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var shortId1 = guid.ToShortId();
        var shortId2 = guid.ToShortId();
        
        Assert.That(shortId1, Is.EqualTo(shortId2));
    }

    [Test]
    public void ToShortId_DifferentGuids_ProduceDifferentResults()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var shortId1 = guid1.ToShortId();
        var shortId2 = guid2.ToShortId();
        
        Assert.That(shortId1, Is.Not.EqualTo(shortId2));
    }

    [Test]
    public void ToSlugUrl_CreatesUrlSafeString()
    {
        var guid = Guid.NewGuid();
        var slug = guid.ToSlugUrl();
        
        Assert.That(slug, Is.Not.Null);
        Assert.That(slug.Length, Is.EqualTo(22));
        Assert.That(slug, Does.Not.Contain("/"));
        Assert.That(slug, Does.Not.Contain("+"));
        Assert.That(slug, Does.Not.Contain("="));
    }

    [Test]
    public void ToSlugUrl_UsesUrlSafeCharacters()
    {
        var guid = Guid.NewGuid();
        var slug = guid.ToSlugUrl();
        
        // Should only contain alphanumeric, dash, and underscore
        var validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
        foreach (var c in slug)
        {
            Assert.That(validChars.Contains(c), Is.True, $"Character '{c}' is not URL safe");
        }
    }

    [Test]
    public void ToSlugUrl_SameGuid_ProducesSameResult()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var slug1 = guid.ToSlugUrl();
        var slug2 = guid.ToSlugUrl();
        
        Assert.That(slug1, Is.EqualTo(slug2));
    }

    [Test]
    public void ToSlugUrl_DifferentGuids_ProduceDifferentResults()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var slug1 = guid1.ToSlugUrl();
        var slug2 = guid2.ToSlugUrl();
        
        Assert.That(slug1, Is.Not.EqualTo(slug2));
    }
}
