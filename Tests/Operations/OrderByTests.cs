using CoreOne.Operations;

namespace Tests.Operations;

[TestFixture]
public class OrderByTests
{
    [Test]
    public void Constructor_WithStringDirection_Ascending()
    {
        var orderBy = new OrderBy("Name", "Ascending");

        Assert.Multiple(() =>
        {
            Assert.That(orderBy.Field, Is.EqualTo("Name"));
            Assert.That(orderBy.Direction, Is.EqualTo(SortDirection.Ascending));
        });
    }

    [Test]
    public void Constructor_WithStringDirection_ASC()
    {
        var orderBy = new OrderBy("Age", "ASC");

        Assert.That(orderBy.Direction, Is.EqualTo(SortDirection.Ascending));
    }

    [Test]
    public void Constructor_WithStringDirection_Descending()
    {
        var orderBy = new OrderBy("Date", "Descending");

        Assert.That(orderBy.Direction, Is.EqualTo(SortDirection.Descending));
    }

    [Test]
    public void Constructor_WithStringDirection_Default()
    {
        var orderBy = new OrderBy("Field", "Unknown");

        Assert.That(orderBy.Direction, Is.EqualTo(SortDirection.Descending));
    }

    [Test]
    public void Constructor_WithEnumDirection()
    {
        var orderBy = new OrderBy("Name", SortDirection.Ascending);

        Assert.Multiple(() =>
        {
            Assert.That(orderBy.Field, Is.EqualTo("Name"));
            Assert.That(orderBy.Direction, Is.EqualTo(SortDirection.Ascending));
        });
    }

    [Test]
    public void Constructor_WithNullField_SetsEmptyString()
    {
        var orderBy = new OrderBy(null!, SortDirection.Ascending);

        Assert.That(orderBy.Field, Is.Empty);
    }

    [Test]
    public void Ascending_CreatesAscendingSort()
    {
        var orderBy = OrderBy.Ascending("Price");

        Assert.Multiple(() =>
        {
            Assert.That(orderBy.Field, Is.EqualTo("Price"));
            Assert.That(orderBy.Direction, Is.EqualTo(SortDirection.Ascending));
        });
    }

    [Test]
    public void Descending_CreatesDescendingSort()
    {
        var orderBy = OrderBy.Descending("Score");

        Assert.Multiple(() =>
        {
            Assert.That(orderBy.Field, Is.EqualTo("Score"));
            Assert.That(orderBy.Direction, Is.EqualTo(SortDirection.Descending));
        });
    }

    [Test]
    public void Equals_WithSameFieldAndDirection_ReturnsTrue()
    {
        var order1 = new OrderBy("Name", SortDirection.Ascending);
        var order2 = new OrderBy("Name", SortDirection.Ascending);

        Assert.That(order1.Equals(order2), Is.True);
    }

    [Test]
    public void Equals_WithDifferentField_ReturnsFalse()
    {
        var order1 = new OrderBy("Name", SortDirection.Ascending);
        var order2 = new OrderBy("Age", SortDirection.Ascending);

        Assert.That(order1.Equals(order2), Is.False);
    }

    [Test]
    public void Equals_WithDifferentDirection_ReturnsFalse()
    {
        var order1 = new OrderBy("Name", SortDirection.Ascending);
        var order2 = new OrderBy("Name", SortDirection.Descending);

        Assert.That(order1.Equals(order2), Is.False);
    }

    [Test]
    public void Equals_CaseInsensitive()
    {
        var order1 = new OrderBy("NAME", SortDirection.Ascending);
        var order2 = new OrderBy("name", SortDirection.Ascending);

        Assert.That(order1.Equals(order2), Is.True);
    }

    [Test]
    public void GetHashCode_ConsistentForSameValues()
    {
        var order1 = new OrderBy("Name", SortDirection.Ascending);
        var order2 = new OrderBy("Name", SortDirection.Ascending);

        Assert.That(order1.GetHashCode(), Is.EqualTo(order2.GetHashCode()));
    }

    [Test]
    public void ToString_ReturnsFieldAndDirection()
    {
        var orderBy = new OrderBy("Title", SortDirection.Descending);

        var result = orderBy.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Contain("Title"));
            Assert.That(result, Does.Contain("Descending"));
        });
    }
}
