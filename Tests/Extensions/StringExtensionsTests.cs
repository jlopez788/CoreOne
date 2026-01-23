using CoreOne.Extensions;
using NUnit.Framework;

namespace Tests.Extensions;

public class StringExtensionsTests
{
    [Test]
    public void ContainsX_WithMatchingStrings_ReturnsTrue()
    {
        var value = "Hello World";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(value.ContainsX("world"), Is.True);
            Assert.That(value.ContainsX("HELLO"), Is.True);
            Assert.That(value.ContainsX("Hello"), Is.True);
        }
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(nullValue.ContainsX("test"), Is.False);
            Assert.That("".ContainsX("test"), Is.False);
            Assert.That("test".ContainsX(null), Is.False);
        }
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That("Hello".Matches("hello"), Is.True);
            Assert.That("WORLD".Matches("world"), Is.True);
            Assert.That("Test".Matches("Different"), Is.False);
        }
    }

    [Test]
    public void Matches_HandlesNullValues()
    {
        string? nullValue = null;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(nullValue.Matches(null), Is.True);
            Assert.That(nullValue.Matches("test"), Is.False);
            Assert.That("test".Matches(null), Is.False);
        }
    }

    [Test]
    public void MatchesAny_FindsMatchInList()
    {
        var value = "apple";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(value.MatchesAny("banana", "APPLE", "orange"), Is.True);
            Assert.That(value.MatchesAny("banana", "orange"), Is.False);
        }
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
        Assert.That(result, Has.Count.EqualTo(3));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0], Is.EqualTo("apple"));
            Assert.That(result[1], Is.EqualTo("banana"));
            Assert.That(result[2], Is.EqualTo("orange"));
        }
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

    [Test]
    public void ContainsX_WithCaseSensitiveComparison_WorksCorrectly()
    {
        var value = "Hello World";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(value.ContainsX("Hello", StringComparison.Ordinal), Is.True);
            Assert.That(value.ContainsX("hello", StringComparison.Ordinal), Is.False);
        }
    }

    [Test]
    public void Matches_WithCaseSensitiveComparison_WorksCorrectly()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That("Test".Matches("test", StringComparison.Ordinal), Is.False);
            Assert.That("Test".Matches("Test", StringComparison.Ordinal), Is.True);
        }
    }

    [Test]
    public void MatchesAny_WithEmptyArray_ReturnsFalse()
    {
        var value = "test";
        Assert.That(value.MatchesAny(), Is.False);
    }

    [Test]
    public void MatchesAny_WithNullValue_ReturnsFalse()
    {
        string? value = null;
        Assert.That(value.MatchesAny("test", "hello"), Is.False);
    }

    [Test]
    public void MatchesAny_WithCustomComparison_WorksCorrectly()
    {
        var value = "Test";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(value.MatchesAny(StringComparison.Ordinal, "test", "TEST"), Is.False);
            Assert.That(value.MatchesAny(StringComparison.Ordinal, "Test", "hello"), Is.True);
        }
    }

    [Test]
    public void Remove_WithNullValue_ReturnsEmptyString()
    {
        string? value = null;
        var result = StringExtensions.Remove(value, '-');
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void Remove_WithNoMatchingCharacters_ReturnsOriginal()
    {
        var value = "hello";
        var result = StringExtensions.Remove(value, '-', '_');
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void Remove_WithEmptyString_ReturnsEmptyString()
    {
        var value = "";
        var result = StringExtensions.Remove(value, '-');
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void SplitBy_TrimsWhitespace()
    {
        var value = "  apple  ,  banana  ";
        var result = value.SplitBy([',']).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0], Is.EqualTo("apple"));
            Assert.That(result[1], Is.EqualTo("banana"));
        }
    }

    [Test]
    public void SplitBy_RemovesEmptyEntries()
    {
        var value = "apple,,banana";
        var result = value.SplitBy([',']).ToList();
        
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void Separate_WithDashSeparator_UsesCorrectFormat()
    {
        var value = "TestCaseValue";
        var result = value.Separate("-");
        Assert.That(result, Is.EqualTo("test-case-value"));
    }

    [Test]
    public void Separate_WithEmptyString_ReturnsEmpty()
    {
        var value = "";
        var result = value.Separate(" ");
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void Separate_WithNumbersAndLetters_HandlesCorrectly()
    {
        var value = "Test123Value";
        var result = value.Separate(" ");
        Assert.That(result, Is.EqualTo("test123 value"));
    }

    [Test]
    public void Separate_WithAllUpperCase_KeepsLettersTogether()
    {
        var value = "HTTP";
        var result = value.Separate(" ");
        Assert.That(result, Is.EqualTo("http"));
    }

    [Test]
    public void ToXString_WithNullModel_ReturnsEmpty()
    {
        int? model = null;
        var result = model.ToXString();
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ToXString_WithFormat_FormatsCorrectly()
    {
        var date = new DateTime(2026, 1, 22);
        var result = date.ToXString("yyyy-MM-dd");
        Assert.That(result, Is.EqualTo("2026-01-22"));
    }

    [Test]
    public void ToXString_WithDefaultValue_UsesDefault()
    {
        int? nullInt = null;
        var result = nullInt.ToXString(usedefault: true);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void EndWith_WithEmptyString_ReturnsEndwith()
    {
        var value = "";
        var result = value.EndWith(".txt");
        Assert.That(result, Is.EqualTo(".txt"));
    }

    [Test]
    public void Matches_BothNull_ReturnsTrue()
    {
        string? val1 = null;
        string? val2 = null;
        Assert.That(val1.Matches(val2), Is.True);
    }
}
