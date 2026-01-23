using CoreOne.Operations;

namespace Tests.Operations;

[TestFixture]
public class PageRequestTests
{
    [Test]
    public void DefaultConstructor_InitializesDefaults()
    {
        var request = new PageRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.CurrentPage, Is.EqualTo(0));
            Assert.That(request.PageSize, Is.EqualTo(0));
            Assert.That(request.Operations, Is.Not.Null);
            Assert.That(request.Operations, Is.Empty);
            Assert.That(request.Token, Is.Not.Null);
        });
    }

    [Test]
    public void ParameterizedConstructor_SetsPageSizeAndCurrentPage()
    {
        var request = new PageRequest(5, 20);

        Assert.Multiple(() =>
        {
            Assert.That(request.CurrentPage, Is.EqualTo(5));
            Assert.That(request.PageSize, Is.EqualTo(20));
            Assert.That(request.Operations, Is.Empty);
        });
    }

    [Test]
    public void Add_AddsOperation_ReturnsThis()
    {
        var request = new PageRequest();
        var operation = new OrderBy("Name", SortDirection.Ascending);

        var result = request.Add(operation);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(request));
            Assert.That(request.Operations, Has.Count.EqualTo(1));
            Assert.That(request.Operations[0], Is.SameAs(operation));
        });
    }

    [Test]
    public void Add_WithNullOperation_DoesNotAdd()
    {
        var request = new PageRequest();

        var result = request.Add(null!);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(request));
            Assert.That(request.Operations, Is.Empty);
        });
    }

    [Test]
    public void AddField_AddsAvailableField()
    {
        var request = new PageRequest();

        var result = request.AddField(typeof(string), "Name", "text");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(request));
            Assert.That(request.Operations, Has.Count.EqualTo(1));
            Assert.That(request.Operations[0], Is.TypeOf<AvailableField>());
        });
    }

    [Test]
    public void AddField_WithNullOrWhitespaceField_DoesNotAdd()
    {
        var request = new PageRequest();

        request.AddField(typeof(string), "");
        request.AddField(typeof(string), null!);
        request.AddField(typeof(string), "  ");

        Assert.That(request.Operations, Is.Empty);
    }

    [Test]
    public void ApplyOperation_WithFilterBy_UpdatesOrAddsFilter()
    {
        var request = new PageRequest(2, 10);
        var filter = new FilterBy("test", "Name");

        request.ApplyOperation(filter);

        Assert.Multiple(() =>
        {
            Assert.That(request.Operations, Has.Count.EqualTo(1));
            Assert.That(request.Operations[0], Is.TypeOf<FilterBy>());
            Assert.That(request.CurrentPage, Is.EqualTo(1)); // Should reset to page 1
        });
    }

    [Test]
    public void ApplyOperation_WithResetFilter_RemovesMatchingFilters()
    {
        var request = new PageRequest();
        request.Add(new FilterBy("test1", "Name"));
        request.Add(new FilterBy("test2", "Age"));

        request.ApplyOperation(new ResetFilter("Name"));

        Assert.Multiple(() =>
        {
            Assert.That(request.Operations, Has.Count.EqualTo(1));
            var remaining = request.Operations[0] as FilterBy;
            Assert.That(remaining?.Field, Is.EqualTo("Age"));
        });
    }

    [Test]
    public void ApplyOperation_WithMergeFilter_MergesExistingFilter()
    {
        var request = new PageRequest(3, 15);
        var initialFilter = new FilterBy("initial", "Status");
        request.Add(initialFilter);

        var mergeFilter = new MergeFilter("Status", current => new FilterBy("merged", "Status"));
        request.ApplyOperation(mergeFilter);

        Assert.Multiple(() =>
        {
            Assert.That(request.Operations, Has.Count.EqualTo(1));
            Assert.That(request.CurrentPage, Is.EqualTo(1)); // Reset to page 1
        });
    }

    [Test]
    public void CancelCurrentTokenThenRenew_CancelsAndCreatesNewToken()
    {
        var request = new PageRequest();
        var originalToken = request.Token;

        request.CancelCurrentTokenThenRenew();

        Assert.Multiple(() =>
        {
            Assert.That(originalToken.IsCancellationRequested, Is.True);
            Assert.That(request.Token, Is.Not.SameAs(originalToken));
            Assert.That(request.Token.IsCancellationRequested, Is.False);
        });
    }

    [Test]
    public void ClearOperations_RemovesMatchingOperations()
    {
        var request = new PageRequest();
        request.Add(new FilterBy("test", "Name"));
        request.Add(new OrderBy("Age", SortDirection.Descending));

        var result = request.ClearOperations(op => op is FilterBy);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(request));
            Assert.That(request.Operations, Has.Count.EqualTo(1));
            Assert.That(request.Operations[0], Is.TypeOf<OrderBy>());
        });
    }

    [Test]
    public void ClearOperations_WithNullPredicate_DoesNothing()
    {
        var request = new PageRequest();
        request.Add(new FilterBy("test", "Name"));

        request.ClearOperations(null!);

        Assert.That(request.Operations, Has.Count.EqualTo(1));
    }

    [Test]
    public void FilterBy_AddsFilterAndResetsPage()
    {
        var request = new PageRequest(5, 20);

        var result = request.FilterBy("searchValue", "Name");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(request));
            Assert.That(request.CurrentPage, Is.EqualTo(1));
            Assert.That(request.Operations, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void FilterBy_WithEmptyValue_DoesNotAddFilter()
    {
        var request = new PageRequest();

        request.FilterBy("", "Name");
        request.FilterBy(null, "Name");

        Assert.That(request.Operations, Is.Empty);
    }
}
