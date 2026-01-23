using CoreOne.Operations;

namespace Tests.Operations;

[TestFixture]
public class FilterByTests
{
    [Test]
    public void DefaultConstructor_InitializesEmptyValues()
    {
        var filter = new FilterBy();

        Assert.Multiple(() =>
        {
            Assert.That(filter.Field, Is.Empty);
            Assert.That(filter.Value, Is.Null);
            Assert.That(filter.Id, Is.Not.Empty);
            Assert.That(filter.AdvancedSearch, Is.Null);
        });
    }

    [Test]
    public void Constructor_WithValueAndField_SetsProperties()
    {
        var filter = new FilterBy("test value", "Name");

        Assert.Multiple(() =>
        {
            Assert.That(filter.Field, Is.EqualTo("Name"));
            Assert.That(filter.Value, Is.EqualTo("test value"));
            Assert.That(filter.Id, Is.Not.Empty);
        });
    }

    [Test]
    public void Constructor_WithNullField_SetsEmptyString()
    {
        var filter = new FilterBy("value", null);

        Assert.That(filter.Field, Is.Empty);
    }

    [Test]
    public void Equals_WithSameFieldAndValue_ReturnsTrue()
    {
        var filter1 = new FilterBy("test", "Name");
        var filter2 = new FilterBy("test", "Name");

        Assert.That(filter1, Is.EqualTo(filter2));
    }

    [Test]
    public void Equals_WithDifferentFieldOrValue_ReturnsFalse()
    {
        var filter1 = new FilterBy("test", "Name");
        var filter2 = new FilterBy("test", "Age");
        var filter3 = new FilterBy("different", "Name");

        Assert.Multiple(() =>
        {
            Assert.That(filter1, Is.Not.EqualTo(filter2));
            Assert.That(filter1, Is.Not.EqualTo(filter3));
        });
    }

    [Test]
    public void Equals_WithNull_ReturnsFalse()
    {
        var filter = new FilterBy("test", "Name");

        Assert.That(filter, Is.Not.Null);
    }

    [Test]
    public void Equals_CaseInsensitive()
    {
        var filter1 = new FilterBy("Test", "NAME");
        var filter2 = new FilterBy("test", "name");

        Assert.That(filter1, Is.EqualTo(filter2));
    }

    [Test]
    public void GetHashCode_ConsistentForSameValues()
    {
        var filter1 = new FilterBy("test", "Name");
        var filter2 = new FilterBy("test", "Name");

        Assert.That(filter1.GetHashCode(), Is.EqualTo(filter2.GetHashCode()));
    }

    [Test]
    public void ToString_ReturnsFieldAndValue()
    {
        var filter = new FilterBy("searchTerm", "Title");

        var result = filter.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Contain("Title"));
            Assert.That(result, Does.Contain("searchTerm"));
        });
    }

    [Test]
    public void ToString_WithNoValue_ReturnsFieldOnly()
    {
        var filter = new FilterBy(null!, "Field");

        var result = filter.ToString();

        Assert.That(result, Does.Contain("Field"));
    }
}
