using CoreOne.Extensions;
using NUnit.Framework;

namespace Tests.Extensions;

public class StringExtensionsTests
{
    [Test]
    public void ContainsX_WithMatchingStrings_ReturnsTrue()
    {
        var value = "Hello World";
        Assert.That(value.ContainsX("world"), Is.True);
        Assert.That(value.ContainsX("HELLO"), Is.True);
        Assert.That(value.ContainsX("Hello"), Is.True);
    }

    [Test]
    public void ContainsX_WithNonMatchingStrings_ReturnsFalse()
    {
        var value = "Hello World";
        Assert.That(value.ContainsX("xyz"), Is.False);
    }

    [Test]
    public void ContainsX_WithNullOrEmpty_ReturnsFalse()
    {
        string? nullValue = null;
        Assert.That(nullValue.ContainsX("test"), Is.False);
        Assert.That("".ContainsX("test"), Is.False);
        Assert.That("test".ContainsX(null), Is.False);
    }

    [Test]
    public void EndWith_AddsEndString_WhenNotPresent()
    {
        var value = "test";
        var result = value.EndWith(".txt");
        Assert.That(result, Is.EqualTo("test.txt"));
    }

    [Test]
    public void EndWith_DoesNotAddEndString_WhenAlreadyPresent()
    {
        var value = "test.txt";
        var result = value.EndWith(".txt");
        Assert.That(result, Is.EqualTo("test.txt"));
    }

    [Test]
    public void EndWith_HandlesNullString()
    {
        string? value = null;
        var result = value.EndWith(".txt");
        Assert.That(result, Is.EqualTo(".txt"));
    }

    [Test]
    public void Matches_ComparesStrings_CaseInsensitive()
    {
        Assert.That("Hello".Matches("hello"), Is.True);
        Assert.That("WORLD".Matches("world"), Is.True);
        Assert.That("Test".Matches("Different"), Is.False);
    }

    [Test]
    public void Matches_HandlesNullValues()
    {
        string? nullValue = null;
        Assert.That(nullValue.Matches(null), Is.True);
        Assert.That(nullValue.Matches("test"), Is.False);
        Assert.That("test".Matches(null), Is.False);
    }

    [Test]
    public void MatchesAny_FindsMatchInList()
    {
        var value = "apple";
        Assert.That(value.MatchesAny("banana", "APPLE", "orange"), Is.True);
        Assert.That(value.MatchesAny("banana", "orange"), Is.False);
    }

    [Test]
    public void Remove_RemovesSpecifiedCharacters()
    {
        var value = "a-b-c-d";
        var result = StringExtensions.Remove(value, '-');
        Assert.That(result, Is.EqualTo("abcd"));
    }

    [Test]
    public void Remove_RemovesMultipleCharacters()
    {
        var value = "a-b_c:d";
        // Note: Array.BinarySearch requires sorted array
        char[] toRemove = ['-', '_', ':'];
        Array.Sort(toRemove);
        var result = StringExtensions.Remove(value, toRemove);
        Assert.That(result, Is.EqualTo("abcd"));
    }

    [Test]
    public void SplitBy_SplitsStringByDelimiters()
    {
        var value = "apple,banana;orange";
        var result = value.SplitBy([',', ';']).ToList();
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result[0], Is.EqualTo("apple"));
        Assert.That(result[1], Is.EqualTo("banana"));
        Assert.That(result[2], Is.EqualTo("orange"));
    }

    [Test]
    public void SplitBy_HandlesNullString()
    {
        string? value = null;
        var result = value.SplitBy([',']);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Separate_InsertsSpacesInPascalCase()
    {
        var value = "HelloWorldTest";
        var result = value.Separate(" ");
        Assert.That(result, Is.EqualTo("hello world test"));
    }

    [Test]
    public void Separate_HandlesConsecutiveUpperCase()
    {
        var value = "XMLParser";
        var result = value.Separate(" ");
        Assert.That(result, Is.EqualTo("xml parser"));
    }

    [Test]
    public void Separate_HandlesSingleWord()
    {
        var value = "hello";
        var result = value.Separate(" ");
        Assert.That(result, Is.EqualTo("hello"));
    }
}
