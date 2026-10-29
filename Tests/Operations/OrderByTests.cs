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

        Assert.That(order1, Is.EqualTo(order2));
    }

    [Test]
    public void Equals_WithDifferentField_ReturnsFalse()
    {
        var order1 = new OrderBy("Name", SortDirection.Ascending);
        var order2 = new OrderBy("Age", SortDirection.Ascending);

        Assert.That(order1, Is.Not.EqualTo(order2));
    }

    [Test]
    public void Equals_WithDifferentDirection_ReturnsFalse()
    {
        var order1 = new OrderBy("Name", SortDirection.Ascending);
        var order2 = new OrderBy("Name", SortDirection.Descending);

        Assert.That(order1, Is.Not.EqualTo(order2));
    }

    [Test]
    public void Equals_CaseInsensitive()
    {
        var order1 = new OrderBy("NAME", SortDirection.Ascending);
        var order2 = new OrderBy("name", SortDirection.Ascending);

        Assert.That(order1, Is.EqualTo(order2));
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

    [Test]
    public void Apply_OrderBy_AddsToListAndReturnsFalse()
    {
        var operations = new List<IOperation>();
        var orderBy = new OrderBy("Name", SortDirection.Ascending);

        var changed = orderBy.Apply(operations);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(changed, Is.False);
            Assert.That(operations, Has.Count.EqualTo(1));
            Assert.That(operations[0], Is.SameAs(orderBy));
        }
    }

    [Test]
    public void Apply_SingleOrderBy_RemovesExistingOrderBysAndReturnsTrue()
    {
        var filter = new FilterBy("Active", "Status");
        var operations = new List<IOperation> {
            filter,
            new OrderBy("Name", SortDirection.Ascending),
            new SingleOrderBy("Age", SortDirection.Descending)
        };
        var single = new SingleOrderBy("CreatedDate", "ASC");

        var changed = single.Apply(operations);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(changed, Is.True);
            Assert.That(operations, Has.Count.EqualTo(2));
            Assert.That(operations[0], Is.SameAs(filter));
            Assert.That(operations[1], Is.SameAs(single));
            Assert.That(single.Direction, Is.EqualTo(SortDirection.Ascending));
        }
    }

    [Test]
    public void SingleOrderBy_EqualsAndGetHashCode_BehaveCorrectly()
    {
        var single1 = new SingleOrderBy("Name", SortDirection.Ascending);
        var single2 = new SingleOrderBy("name", "Ascending");
        var single3 = new SingleOrderBy("Name", SortDirection.Descending);
        var regularOrder = new OrderBy("Name", SortDirection.Ascending);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(single1, Is.EqualTo(single2));
            Assert.That(single1.GetHashCode(), Is.EqualTo(single2.GetHashCode()));
            Assert.That(single1, Is.Not.EqualTo(single3));
            Assert.That(single1.Equals(regularOrder), Is.False);
            Assert.That(single1.ToString(), Is.EqualTo("Name Ascending"));
        }
    }
}
