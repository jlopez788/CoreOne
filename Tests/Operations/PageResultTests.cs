using CoreOne.Operations;
using CoreOne.Results;

namespace Tests.Operations;

[TestFixture]
public class PageResultTests
{
    [Test]
    public void Constructor_WithPageSize_InitializesDefaults()
    {
        var result = new PageResult<string>(10);

        Assert.Multiple(() =>
        {
            Assert.That(result.PageSize, Is.EqualTo(10));
            Assert.That(result.CurrentPage, Is.EqualTo(1));
            Assert.That(result.Results, Is.Not.Null);
            Assert.That(result.Results, Is.Empty);
            Assert.That(result.TotalCount, Is.EqualTo(0));
            Assert.That(result.PageCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void Constructor_WithData_CalculatesPageCount()
    {
        var data = new[] { "item1", "item2", "item3", "item4", "item5" };

        var result = new PageResult<string>(data, 1, 2, 5);

        Assert.Multiple(() =>
        {
            Assert.That(result.Results, Has.Count.EqualTo(5));
            Assert.That(result.CurrentPage, Is.EqualTo(1));
            Assert.That(result.PageSize, Is.EqualTo(2));
            Assert.That(result.TotalCount, Is.EqualTo(5));
            Assert.That(result.PageCount, Is.EqualTo(3)); // Ceiling(5/2) = 3
            Assert.That(result.ResultType, Is.EqualTo(ResultType.Success));
            Assert.That(result.Success, Is.True);
        });
    }

    [Test]
    public void Constructor_WithExactPageFit_CalculatesCorrectPageCount()
    {
        var data = new[] { 1, 2, 3, 4, 5, 6 };

        var result = new PageResult<int>(data, 2, 3, 6);

        Assert.That(result.PageCount, Is.EqualTo(2)); // 6 items / 3 per page = 2 pages
    }

    [Test]
    public void Constructor_WithLessThanOnePageOfData_ReturnsOnePageCount()
    {
        var data = new[] { "a", "b" };

        var result = new PageResult<string>(data, 1, 10, 2);

        Assert.That(result.PageCount, Is.EqualTo(1));
    }

    [Test]
    public void Constructor_WithZeroPageSize_ReturnsOnePageCount()
    {
        var data = new[] { 1, 2, 3 };

        var result = new PageResult<int>(data, 1, 0, 3);

        Assert.That(result.PageCount, Is.EqualTo(1));
    }

    [Test]
    public void Constructor_WithNullData_InitializesEmptyList()
    {
        var result = new PageResult<string>(null, 1, 10, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Results, Is.Not.Null);
            Assert.That(result.Results, Is.Empty);
            Assert.That(result.Model, Is.Empty);
        });
    }

    [Test]
    public void Model_ReturnsSameAsResults()
    {
        var data = new[] { 1, 2, 3 };
        var result = new PageResult<int>(data, 1, 10, 3);

        Assert.That(result.Model, Is.SameAs(result.Results));
    }

    [Test]
    public void Success_ReturnsTrueWhenResultTypeSuccess()
    {
        var result = new PageResult<int>(new[] { 1 }, 1, 10, 1);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void InheritsFromPageRequest_CanUseOperations()
    {
        var result = new PageResult<string>(10);
        var orderBy = new OrderBy("Name", SortDirection.Ascending);

        result.Add(orderBy);

        Assert.That(result.Operations, Has.Count.EqualTo(1));
    }

    [Test]
    public void PageCount_WithLargeDataset_CalculatesCorrectly()
    {
        var data = Enumerable.Range(1, 100).ToArray();

        var result = new PageResult<int>(data, 1, 15, 100);

        Assert.That(result.PageCount, Is.EqualTo(7)); // Ceiling(100/15) = 7
    }
}
