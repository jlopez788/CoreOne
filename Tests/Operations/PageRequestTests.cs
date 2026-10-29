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
    public void Add_WithFilterBy_UpdatesOrAddsFilter()
    {
        var request = new PageRequest(2, 10);
        var filter = new FilterBy("test", "Name"); 
        request.Add(filter);

        Assert.Multiple(() =>
        {
            Assert.That(request.Operations, Has.Count.EqualTo(1));
            Assert.That(request.Operations[0], Is.TypeOf<FilterBy>());
            Assert.That(request.CurrentPage, Is.EqualTo(1)); // Should reset to page 1
        });
    }

    [Test]
    public void Add_WithFilterBy_UpdatesExistingFilterAdvancedSearchAndResetsPage()
    {
        var request = new PageRequest(4, 10);
        var initial = new FilterBy("test", "Name");
        request.Add(initial);

        request.CurrentPage = 3;
        var advanced = new CoreOne.ODataBuilders.AdvancedFilterContext();
        var updated = new FilterBy(advanced, "name", "test");
        request.Add(updated);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Operations, Has.Count.EqualTo(1));
            var stored = request.Operations[0] as FilterBy;
            Assert.That(stored, Is.SameAs(initial));
            Assert.That(stored?.AdvancedSearch, Is.SameAs(advanced));
            Assert.That(request.CurrentPage, Is.EqualTo(1));
        }
    }

    [Test]
    public void Add_WithResetFilter_RemovesMatchingFiltersAndResetsPage()
    {
        var request = new PageRequest(5, 10);
        request.Add(new FilterBy("test1", "Name"));
        request.Add(new FilterBy("test2", "Age"));
        request.CurrentPage = 4;

        request.Add(new ResetFilter("name"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Operations, Has.Count.EqualTo(1));
            var remaining = request.Operations[0] as FilterBy;
            Assert.That(remaining?.Field, Is.EqualTo("Age"));
            Assert.That(request.CurrentPage, Is.EqualTo(1));
        }
    }

    [Test]
    public void Add_WithMergeFilter_MergesExistingFilter()
    {
        var request = new PageRequest(3, 15);
        var initialFilter = new FilterBy("initial", "Status");
        request.Add(initialFilter);
        request.CurrentPage = 3;

        FilterBy? passedCurrent = null;
        var mergeFilter = new MergeFilter("Status", current =>
        {
            passedCurrent = current;
            return new FilterBy("merged", "Status");
        });
        request.Add(mergeFilter);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(passedCurrent, Is.SameAs(initialFilter));
            Assert.That(request.Operations, Has.Count.EqualTo(1));
            Assert.That(request.GetFilterValue("Status"), Is.EqualTo("merged"));
            Assert.That(request.CurrentPage, Is.EqualTo(1)); // Reset to page 1
        }
    }

    [Test]
    public void Add_WithMergeFilter_WhenNoExistingFilter_PassesNullAndAddsMergedFilter()
    {
        var request = new PageRequest(3, 15);
        FilterBy? passedCurrent = new FilterBy("dummy", "Status");

        var mergeFilter = new MergeFilter("Status", current =>
        {
            passedCurrent = current;
            return new FilterBy("created", "Status");
        });
        request.Add(mergeFilter);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(passedCurrent, Is.Null);
            Assert.That(request.Operations, Has.Count.EqualTo(1));
            Assert.That(request.GetFilterValue("Status"), Is.EqualTo("created"));
            Assert.That(request.CurrentPage, Is.EqualTo(1));
        }
    }

    [Test]
    public void Add_WithSingleOrderBy_RemovesExistingOrderOperationsAndResetsPage()
    {
        var request = new PageRequest(4, 20);
        request.Add(new FilterBy("Alice", "Name"));
        request.CurrentPage = 4;
        request.Add(new OrderBy("Name", SortDirection.Ascending));
        request.Add(new OrderBy("Age", SortDirection.Descending));

        var singleOrder = new SingleOrderBy("CreatedDate", SortDirection.Descending);
        request.Add(singleOrder);

        var sorts = request.GetSortFields().ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.CurrentPage, Is.EqualTo(1));
            Assert.That(sorts, Has.Count.EqualTo(1));
            Assert.That(sorts[0], Is.SameAs(singleOrder));
            Assert.That(request.GetFilters().Count(), Is.EqualTo(1));
        }
    }

    [Test]
    public void Add_WithAvailableField_AddsOperationWithoutResettingPage()
    {
        var request = new PageRequest(4, 20);
        var available = new AvailableField(typeof(string), "Email");

        request.Add(available);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.CurrentPage, Is.EqualTo(4));
            Assert.That(request.GetAvailableFields(), Contains.Item(available));
        }
    }

    [Test]
    public void OrderBy_Multiple_AddsOrderByWithoutResettingPage()
    {
        var request = new PageRequest(3, 10);

        request.OrderBy("Name", SortDirection.Ascending)
               .OrderBy("Age", SortDirection.Descending, single: false);

        var sorts = request.GetSortFields().ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.CurrentPage, Is.EqualTo(3));
            Assert.That(sorts, Has.Count.EqualTo(2));
            Assert.That(sorts[0], Is.TypeOf<OrderBy>());
            Assert.That(sorts[0].Field, Is.EqualTo("Name"));
            Assert.That(sorts[1], Is.TypeOf<OrderBy>());
            Assert.That(sorts[1].Field, Is.EqualTo("Age"));
        }
    }

    [Test]
    public void OrderBy_Single_ReplacesAllSortOperationsWithSingleOrderByAndResetsPage()
    {
        var request = new PageRequest(5, 10);
        request.OrderBy("Name", SortDirection.Ascending)
               .OrderBy("Age", SortDirection.Descending);

        request.OrderBy("UpdatedAt", SortDirection.Descending, single: true);

        var sorts = request.GetSortFields().ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.CurrentPage, Is.EqualTo(1));
            Assert.That(sorts, Has.Count.EqualTo(1));
            Assert.That(sorts[0], Is.TypeOf<SingleOrderBy>());
            Assert.That(sorts[0].Field, Is.EqualTo("UpdatedAt"));
            Assert.That(sorts[0].Direction, Is.EqualTo(SortDirection.Descending));
        }
    }

    [Test]
    public void OrderBy_SingleCalledTwice_ReplacesPreviousSingleOrderBy()
    {
        var request = new PageRequest(2, 10);
        request.OrderBy("First", SortDirection.Ascending, single: true);
        request.CurrentPage = 3;

        request.OrderBy("Second", SortDirection.Descending, single: true);

        var sorts = request.GetSortFields().ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.CurrentPage, Is.EqualTo(1));
            Assert.That(sorts, Has.Count.EqualTo(1));
            Assert.That(sorts[0], Is.TypeOf<SingleOrderBy>());
            Assert.That(sorts[0].Field, Is.EqualTo("Second"));
        }
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

    [Test]
    public void TryGetFilter_WhenStringFilterExists_ReturnsTrueAndValue()
    {
        var request = new PageRequest()
            .FilterBy("Alice", "Name");

        var found = request.TryGetFilter<string>("Name", out var value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.True);
            Assert.That(value, Is.EqualTo("Alice"));
        }
    }

    [Test]
    public void TryGetFilter_CaseInsensitiveFieldMatch_ReturnsTrueAndValue()
    {
        var request = new PageRequest()
            .FilterBy("42", "UserAge");

        var found = request.TryGetFilter<int>("userage", out var value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.True);
            Assert.That(value, Is.EqualTo(42));
        }
    }

    [Test]
    public void TryGetFilter_WithPrimitiveAndCommonTypes_ParsesCorrectly()
    {
        var expectedGuid = Guid.NewGuid();
        var request = new PageRequest()
            .FilterBy("123", "Count")
            .FilterBy("99.95", "Price")
            .FilterBy("true", "IsActive")
            .FilterBy(expectedGuid.ToString(), "CorrelationId")
            .FilterBy("05/15/2026", "CreatedDate");

        var hasCount = request.TryGetFilter<int>("Count", out var count);
        var hasPrice = request.TryGetFilter<decimal>("Price", out var price);
        var hasActive = request.TryGetFilter<bool>("IsActive", out var isActive);
        var hasGuid = request.TryGetFilter<Guid>("CorrelationId", out var correlationId);
        var hasDate = request.TryGetFilter<DateTime>("CreatedDate", out var createdDate);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(hasCount, Is.True);
            Assert.That(count, Is.EqualTo(123));
            Assert.That(hasPrice, Is.True);
            Assert.That(price, Is.EqualTo(99.95m));
            Assert.That(hasActive, Is.True);
            Assert.That(isActive, Is.True);
            Assert.That(hasGuid, Is.True);
            Assert.That(correlationId, Is.EqualTo(expectedGuid));
            Assert.That(hasDate, Is.True);
            Assert.That(createdDate, Is.EqualTo(new DateTime(2026, 5, 15)));
        }
    }

    [Test]
    public void TryGetFilter_WithNullableType_ParsesCorrectly()
    {
        var request = new PageRequest()
            .FilterBy("77", "OptionalId");

        var found = request.TryGetFilter<int?>("OptionalId", out var value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.True);
            Assert.That(value, Is.EqualTo(77));
        }
    }

    [Test]
    public void TryGetFilter_WhenFilterDoesNotExist_ReturnsFalseAndDefault()
    {
        var request = new PageRequest()
            .FilterBy("Alice", "Name");

        var foundInt = request.TryGetFilter<int>("MissingInt", out var intValue);
        var foundStr = request.TryGetFilter<string>("MissingStr", out var strValue);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(foundInt, Is.False);
            Assert.That(intValue, Is.EqualTo(0));
            Assert.That(foundStr, Is.False);
            Assert.That(strValue, Is.Null);
        }
    }

    [Test]
    public void TryGetFilter_WhenValueCannotBeParsed_ReturnsFalseAndDefault()
    {
        var request = new PageRequest()
            .FilterBy("not-a-number", "Age")
            .FilterBy("not-a-guid", "Id");

        var foundAge = request.TryGetFilter<int>("Age", out var age);
        var foundId = request.TryGetFilter<Guid>("Id", out var id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(foundAge, Is.False);
            Assert.That(age, Is.EqualTo(0));
            Assert.That(foundId, Is.False);
            Assert.That(id, Is.EqualTo(Guid.Empty));
        }
    }

    [Test]
    public void TryGetFilter_WhenFilterHasNullValue_ReturnsFalseAndDefault()
    {
        var request = new PageRequest();
        request.Add(new FilterBy(null!, "EmptyFilter"));

        var found = request.TryGetFilter<int>("EmptyFilter", out var value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.False);
            Assert.That(value, Is.EqualTo(0));
        }
    }

    [Test]
    public void TryGetFilter_IgnoresNonFilterOperationsWithSameField()
    {
        var request = new PageRequest()
            .AddField(typeof(int), "Age")
            .OrderBy("Age", SortDirection.Ascending);

        var found = request.TryGetFilter<int>("Age", out var value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.False);
            Assert.That(value, Is.EqualTo(0));
        }
    }

    [Test]
    public void FilterBy_WithEmptyValueWhenFilterExists_RemovesFilterForField()
    {
        var request = new PageRequest(3, 10)
            .FilterBy("Alice", "Name")
            .FilterBy("Active", "Status");
        request.CurrentPage = 2;

        request.FilterBy("", "Name");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.CurrentPage, Is.EqualTo(1));
            Assert.That(request.GetFilterValue("Name"), Is.Null);
            Assert.That(request.GetFilterValue("Status"), Is.EqualTo("Active"));
        }
    }

    [Test]
    public void GetHashCode_WithSameStateAndOperations_ReturnsSameHash()
    {
        var request1 = new PageRequest(1, 20)
            .FilterBy("Alice", "Name")
            .OrderBy("Name", SortDirection.Ascending);
        var request2 = new PageRequest(1, 20)
            .FilterBy("Alice", "Name")
            .OrderBy("Name", SortDirection.Ascending);
        var request3 = new PageRequest(1, 50)
            .FilterBy("Alice", "Name")
            .OrderBy("Name", SortDirection.Ascending);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request1.GetHashCode(), Is.Not.EqualTo(0));
            Assert.That(request1.GetHashCode(), Is.EqualTo(request2.GetHashCode()));
            Assert.That(request1.GetHashCode(), Is.Not.EqualTo(request3.GetHashCode()));
        }
    }

    [Test]
    public void UseToken_LinksCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var request = new PageRequest().UseToken(cts.Token);

        cts.Cancel();

        Assert.That(request.Token.IsCancellationRequested, Is.True);
    }

    [Test]
    public void Result_CreatesPageResultWithCopiedOperationsAndToken()
    {
        var request = new PageRequest(2, 10)
            .OrderBy("Name", SortDirection.Ascending);
        var items = new[] { "A", "B" };

        var result = request.Result(items, 25);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.CurrentPage, Is.EqualTo(2));
            Assert.That(result.PageSize, Is.EqualTo(10));
            Assert.That(result.TotalCount, Is.EqualTo(25));
            Assert.That(result.Items, Has.Count.EqualTo(2));
            Assert.That(result.Operations, Has.Count.EqualTo(1));
            Assert.That(result.Token, Is.SameAs(request.Token));
        }
    }
}
