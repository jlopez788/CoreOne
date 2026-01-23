using CoreOne.Extensions;
using CoreOne.Operations;

namespace Tests.Extensions;

public class QueryExtensionsTests
{
    private class TestItem
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
        public DateTime Date { get; set; }
    }

    private static IQueryable<TestItem> GetTestData() => new List<TestItem>
        {
            new() { Name = "A", Value = 10, Date = new DateTime(2024, 1, 1) },
            new() { Name = "B", Value = 20, Date = new DateTime(2024, 2, 1) },
            new() { Name = "C", Value = 15, Date = new DateTime(2024, 1, 15) },
            new() { Name = "D", Value = 25, Date = new DateTime(2024, 3, 1) },
            new() { Name = "E", Value = 5, Date = new DateTime(2024, 1, 20) }
        }.AsQueryable();

    [Test]
    public void OrderBy_AscendingByValue_SortsCorrectly()
    {
        var data = GetTestData();
        var orderBy = new List<OrderBy> { new("Value", SortDirection.Ascending) };
        
        var result = data.OrderBy(orderBy).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Value, Is.EqualTo(5));
            Assert.That(result[4].Value, Is.EqualTo(25));
        }
    }

    [Test]
    public void OrderBy_DescendingByValue_SortsCorrectly()
    {
        var data = GetTestData();
        var orderBy = new List<OrderBy> { new("Value", SortDirection.Descending) };
        
        var result = data.OrderBy(orderBy).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Value, Is.EqualTo(25));
            Assert.That(result[4].Value, Is.EqualTo(5));
        }
    }

    [Test]
    public void OrderBy_MultipleFields_SortsCorrectly()
    {
        var data = GetTestData();
        var orderBy = new List<OrderBy>
        {
            new("Date", SortDirection.Ascending),
            new("Value", SortDirection.Descending)
        };
        
        var result = data.OrderBy(orderBy).ToList();
        
        Assert.That(result[0].Date.Month, Is.EqualTo(1));
    }

    [Test]
    public void OrderBy_StringField_AscendingByName_SortsCorrectly()
    {
        var data = GetTestData();
        var orderBy = new List<OrderBy> { new("Name", SortDirection.Ascending) };
        
        var result = data.OrderBy(orderBy).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Name, Is.EqualTo("A"));
            Assert.That(result[4].Name, Is.EqualTo("E"));
        }
    }

    [Test]
    public void OrderBy_WithString_ParsesAndSorts()
    {
        var data = GetTestData();
        
        var result = data.OrderBy("Value asc").ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Value, Is.EqualTo(5));
            Assert.That(result[4].Value, Is.EqualTo(25));
        }
    }

    [Test]
    public void OrderBy_EmptyList_ReturnsUnchanged()
    {
        var data = GetTestData();
        var orderBy = new List<OrderBy>();
        
        var result = data.OrderBy(orderBy).ToList();
        
        Assert.That(result, Has.Count.EqualTo(5));
    }

    [Test]
    public void Paginate_FirstPage_ReturnsCorrectItems()
    {
        var data = GetTestData();
        var request = new PageRequest { CurrentPage = 1, PageSize = 2 };
        
        var result = data.Paginate(request).ToList();
        
        Assert.That(result, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Name, Is.EqualTo("A"));
            Assert.That(result[1].Name, Is.EqualTo("B"));
        }
    }

    [Test]
    public void Paginate_SecondPage_ReturnsCorrectItems()
    {
        var data = GetTestData();
        var request = new PageRequest { CurrentPage = 2, PageSize = 2 };
        
        var result = data.Paginate(request).ToList();
        
        Assert.That(result, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Name, Is.EqualTo("C"));
            Assert.That(result[1].Name, Is.EqualTo("D"));
        }
    }

    [Test]
    public void Paginate_LastPage_ReturnsRemainingItems()
    {
        var data = GetTestData();
        var request = new PageRequest { CurrentPage = 3, PageSize = 2 };
        
        var result = data.Paginate(request).ToList();
        
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("E"));
    }

    [Test]
    public void Paginate_PageSizeZero_ReturnsAll()
    {
        var data = GetTestData();
        var request = new PageRequest { CurrentPage = 1, PageSize = 0 };
        
        var result = data.Paginate(request).ToList();
        
        Assert.That(result, Has.Count.EqualTo(5));
    }

    [Test]
    public void Paginate_LargePageSize_ReturnsAll()
    {
        var data = GetTestData();
        var request = new PageRequest { CurrentPage = 1, PageSize = 100 };
        
        var result = data.Paginate(request).ToList();
        
        Assert.That(result, Has.Count.EqualTo(5));
    }

    [Test]
    public void Paginate_BeyondLastPage_ReturnsEmpty()
    {
        var data = GetTestData();
        var request = new PageRequest { CurrentPage = 10, PageSize = 2 };
        
        var result = data.Paginate(request).ToList();
        
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void OrderBy_ThenPaginate_WorksTogether()
    {
        var data = GetTestData();
        var orderBy = new List<OrderBy> { new("Value", SortDirection.Ascending) };
        var request = new PageRequest { CurrentPage = 1, PageSize = 3 };
        
        var result = data.OrderBy(orderBy).Paginate(request).ToList();
        
        Assert.That(result, Has.Count.EqualTo(3));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Value, Is.EqualTo(5));
            Assert.That(result[2].Value, Is.EqualTo(15));
        }
    }
}
