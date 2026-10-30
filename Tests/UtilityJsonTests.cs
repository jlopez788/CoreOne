using CoreOne;
using CoreOne.Extensions;
using CoreOne.ODataBuilders;
using CoreOne.Operations;
using CoreOne.Reflection;
using NUnit.Framework;
using System.Text;

namespace Tests;

public class UtilityJsonTests
{
    public class TestModel
    {
        public DateTime Created { get; set; }
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class TypekeyModel
    {
        public TypeKey Key { get; set; }
    }

    [Test]
    public void Deserialize_NonGeneric_DeserializesType()
    {
        var json = "{\"Id\":1,\"Name\":\"Test\"}";
        var obj = Utility.Deserialize(typeof(TestModel), json);

        Assert.That(obj, Is.Not.Null);
        Assert.That(obj, Is.InstanceOf<TestModel>());
        var model = (TestModel)obj!;
        Assert.That(model.Id, Is.EqualTo(1));
    }

    [Test]
    public void DeserializeObject_DeserializesJson()
    {
        var json = "{\"Id\":1,\"Name\":\"Test\"}";
        var model = Utility.DeserializeObject<TestModel>(json);

        Assert.That(model, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(model!.Id, Is.EqualTo(1));
            Assert.That(model.Name, Is.EqualTo("Test"));
        }
    }

    [Test]
    public void DeserializeObject_EmptyJson_ReturnsDefault()
    {
        var model = Utility.DeserializeObject<TestModel>("");

        Assert.That(model, Is.Null);
    }

    [Test]
    public void DeserializeObject_FromStream_ReadsCorrectly()
    {
        var json = "{\"Id\":1,\"Name\":\"Test\"}";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var result = Utility.DeserializeObject<TestModel>(stream);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Model, Is.Not.Null);
            Assert.That(result.Model!.Id, Is.EqualTo(1));
            Assert.That(result.Model.Name, Is.EqualTo("Test"));
        }
    }

    [Test]
    public void DeserializeObject_NullJson_ReturnsDefault()
    {
        var model = Utility.DeserializeObject<TestModel>((string?)null);

        Assert.That(model, Is.Null);
    }

    [Test]
    public void Serialize_NullObject_ReturnsEmpty()
    {
        TestModel? model = null;
        var json = Utility.Serialize(model);

        Assert.That(json, Is.Empty);
    }

    [Test]
    public void Serialize_SerializesObject()
    {
        var model = new TestModel { Id = 1, Name = "Test", Created = new DateTime(2026, 1, 1) };
        var json = Utility.Serialize(model);

        Assert.That(json, Is.Not.Null);
        Assert.That(json, Does.Contain("\"id\":1").Or.Contain("\"Id\":1"));
        Assert.That(json, Does.Contain("\"name\":\"Test\"").Or.Contain("\"Name\":\"Test\""));
    }

    [Test]
    public void Serialize_TypeKey()
    {
        var name = "Test";
        var key = TypeKeyStore.Register<string>(name);
        var json = Utility.Serialize(new TypekeyModel { Key = key });

        Assert.That(json, Is.EqualTo("{\"key\":\"Test\"}"));
        var model = Utility.DeserializeObject<TypekeyModel>(json);
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Key, Is.EqualTo(key));
    }

    [Test]
    public void Serialize_WithPrettyPrint_FormatsJson()
    {
        var model = new TestModel { Id = 1, Name = "Test" };
        var json = Utility.Serialize(model, prettyPrint: true);

        Assert.That(json, Does.Contain("\n"));
        Assert.That(json, Does.Contain("  "));
    }

    [Test]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        var original = new TestModel { Id = 42, Name = "RoundTrip", Created = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc) };
        var json = Utility.Serialize(original);
        var deserialized = Utility.DeserializeObject<TestModel>(json);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(deserialized!.Id, Is.EqualTo(original.Id));
            Assert.That(deserialized.Name, Is.EqualTo(original.Name));
            Assert.That(deserialized.Created, Is.EqualTo(original.Created).Within(TimeSpan.FromSeconds(1)));
        }
    }

    [Test]
    public void SerializeToStream_WithPrettyPrint_FormatsJson()
    {
        var model = new TestModel { Id = 1, Name = "Test" };
        using var stream = new MemoryStream();

        var result = Utility.SerializeToStream(model, stream, prettyPrint: true);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void SerializeToStream_WritesToStream()
    {
        var model = new TestModel { Id = 1, Name = "Test" };
        using var stream = new MemoryStream();

        var result = Utility.SerializeToStream(model, stream);

        Assert.That(result.Success, Is.True);
    }

    [SetUp]
    public void Setup()
    {
        Utility.InitializeSettings();
    }

    [Test]
    public async Task ToStringContent_ContainsSerializedData()
    {
        var model = new TestModel { Id = 1, Name = "Test" };
        var content = model.ToStringContent();
        var json = await content!.ReadAsStringAsync();

        Assert.That(json, Does.Contain("\"id\":1").Or.Contain("\"Id\":1"));
        Assert.That(json, Does.Contain("\"name\":\"Test\"").Or.Contain("\"Name\":\"Test\""));
    }

    [Test]
    public void ToStringContent_CreatesHttpContent()
    {
        var model = new TestModel { Id = 1, Name = "Test" };
        var content = model.ToStringContent();

        Assert.That(content, Is.Not.Null);
        Assert.That(content!.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
    }

    [Test]
    public void ToStringContent_NullModel_ReturnsNull()
    {
        TestModel? model = null;
        var content = model.ToStringContent();

        Assert.That(content, Is.Null);
    }

    [Test]
    public void SerializeDeserialize_PageRequest_WithSingleFilter_ReconstructsFilterCorrectly()
    {
        var request = new PageRequest(2, 25);
        request.Add(new FilterBy("John", "FirstName")); // Once a filter is applied, the CurrentPage is reset to 1

        var json = Utility.Serialize(request, true);
        var deserialized = Utility.DeserializeObject<PageRequest>(json);

        Assert.That(deserialized, Is.Not.Null);
        var filters = deserialized!.GetFilters().ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(deserialized.CurrentPage, Is.EqualTo(1));
            Assert.That(deserialized.PageSize, Is.EqualTo(25));
            Assert.That(filters, Has.Count.EqualTo(1));
            Assert.That(filters[0].Field, Is.EqualTo("FirstName"));
            Assert.That(filters[0].Value, Is.EqualTo("John"));
            Assert.That(deserialized.GetFilterValue("FirstName"), Is.EqualTo("John"));
        }
    }

    [Test]
    public void SerializeDeserialize_PageRequest_WithMultipleFiltersAndTypes_PreservesAllFilterValues()
    {
        var request = new PageRequest(1, 50);
        request.Add(new FilterBy("Active", "Status"));
        request.Add(new FilterBy("42", "Age"));
        request.Add(new FilterBy("true", "IsVerified"));
        request.Add(new FilterBy("global search", ""));

        var json = Utility.Serialize(request, prettyPrint: true);
        var deserialized = Utility.DeserializeObject<PageRequest>(json);

        Assert.That(deserialized, Is.Not.Null);
        var filters = deserialized!.GetFilters().ToList();
        var hasAge = deserialized.TryGetFilter<int>("Age", out var age);
        var hasVerified = deserialized.TryGetFilter<bool>("IsVerified", out var isVerified);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deserialized.CurrentPage, Is.EqualTo(1));
            Assert.That(deserialized.PageSize, Is.EqualTo(50));
            Assert.That(filters, Has.Count.EqualTo(4));
            Assert.That(deserialized.GetFilterValue("Status"), Is.EqualTo("Active"));
            Assert.That(deserialized.GetFilterValue(""), Is.EqualTo("global search"));
            Assert.That(hasAge, Is.True);
            Assert.That(age, Is.EqualTo(42));
            Assert.That(hasVerified, Is.True);
            Assert.That(isVerified, Is.True);
        }
    }

    [Test]
    public void SerializeDeserialize_PageRequest_WithAdvancedFilterContext_ReconstructsAdvancedFilters()
    {
        var request = new PageRequest(3, 15);
        var betweenSearch = new AdvancedFilterContext(ODataOperator.Between, 10, 100);
        var startsWithSearch = new AdvancedFilterContext(ODataOperator.StartsWith, "Admin");
        var notNullSearch = new AdvancedFilterContext(ODataOperator.NotNull);

        request.Add(new FilterBy(betweenSearch, "Price", "10-100"));
        request.Add(new FilterBy(startsWithSearch, "Role", "Admin"));
        request.Add(new FilterBy(notNullSearch, "Email"));

        var json = Utility.Serialize(request, prettyPrint: true);
        var deserialized = Utility.DeserializeObject<PageRequest>(json);

        Assert.That(deserialized, Is.Not.Null);
        var filters = deserialized!.GetFilters().ToList();
        Assert.That(filters, Has.Count.EqualTo(3));

        var priceFilter = filters.FirstOrDefault(f => f.Field == "Price");
        var roleFilter = filters.FirstOrDefault(f => f.Field == "Role");
        var emailFilter = filters.FirstOrDefault(f => f.Field == "Email");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(priceFilter, Is.Not.Null);
            Assert.That(priceFilter!.Value, Is.EqualTo("10-100"));
            Assert.That(priceFilter.AdvancedSearch, Is.Not.Null);
            Assert.That(priceFilter.AdvancedSearch!.Operator, Is.EqualTo(ODataOperator.Between));
            Assert.That(Convert.ToInt32(priceFilter.AdvancedSearch.Value1), Is.EqualTo(10));
            Assert.That(Convert.ToInt32(priceFilter.AdvancedSearch.Value2), Is.EqualTo(100));

            Assert.That(roleFilter, Is.Not.Null);
            Assert.That(roleFilter!.Value, Is.EqualTo("Admin"));
            Assert.That(roleFilter.AdvancedSearch, Is.Not.Null);
            Assert.That(roleFilter.AdvancedSearch!.Operator, Is.EqualTo(ODataOperator.StartsWith));
            Assert.That(roleFilter.AdvancedSearch.Value1, Is.EqualTo("Admin"));

            Assert.That(emailFilter, Is.Not.Null);
            Assert.That(emailFilter!.AdvancedSearch, Is.Not.Null);
            Assert.That(emailFilter.AdvancedSearch!.Operator, Is.EqualTo(ODataOperator.NotNull));
        }
    }

    [Test]
    public void SerializeDeserialize_ComplexPageRequest_WithMixedOperations_ReconstructsAllOperations()
    {
        var request = new PageRequest(4, 20)
            .AddField(typeof(string), "Name", "text")
            .AddField(typeof(int?), "Quantity", "number")
            .AddField(typeof(DateTime), "CreatedDate", "yyyy-MM-dd")
            .Add(new FilterBy("Widget", "Name"))
            .Add(new FilterBy(new AdvancedFilterContext(ODataOperator.GreaterThan, 5), "Quantity", "5"))
            .OrderBy("CreatedDate", SortDirection.Descending)
            .OrderBy("Name", SortDirection.Ascending)
            .Add(new ResetFilter("ObsoleteField"));

        var json = Utility.Serialize(request, prettyPrint: true);
        var deserialized = Utility.DeserializeObject<PageRequest>(json);

        Assert.That(deserialized, Is.Not.Null);
        var availableFields = deserialized!.GetAvailableFields().ToList();
        var filters = deserialized.GetFilters().ToList();
        var sorts = deserialized.GetSortFields().ToList();
        var resets = deserialized.Operations.OfType<ResetFilter>().ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deserialized.CurrentPage, Is.EqualTo(1));
            Assert.That(deserialized.PageSize, Is.EqualTo(20));
            Assert.That(deserialized.Operations, Has.Count.EqualTo(7));

            Assert.That(availableFields, Has.Count.EqualTo(3));
            Assert.That(availableFields[0].Field, Is.EqualTo("Name"));
            Assert.That(availableFields[0].Type, Is.EqualTo(typeof(string)));
            Assert.That(availableFields[1].Field, Is.EqualTo("Quantity"));
            Assert.That(availableFields[1].Type, Is.EqualTo(typeof(int)));
            Assert.That(availableFields[1].IsNullable, Is.True);

            Assert.That(filters, Has.Count.EqualTo(2));
            Assert.That(deserialized.GetFilterValue("Name"), Is.EqualTo("Widget"));
            Assert.That(deserialized.TryGetFilter<int>("Quantity", out var qty), Is.True);
            Assert.That(qty, Is.EqualTo(5));

            Assert.That(sorts, Has.Count.EqualTo(2));
            Assert.That(sorts[0].Field, Is.EqualTo("CreatedDate"));
            Assert.That(sorts[0].Direction, Is.EqualTo(SortDirection.Descending));
            Assert.That(sorts[1].Field, Is.EqualTo("Name"));
            Assert.That(sorts[1].Direction, Is.EqualTo(SortDirection.Ascending));

            Assert.That(resets, Has.Count.EqualTo(0));
        }
    }

    [Test]
    public void SerializeDeserialize_PageRequest_FilterByMethodChain_PreservesLatestFilterState()
    {
        var request = new PageRequest(5, 10)
            .FilterBy("InitialName", "Name")
            .FilterBy("Electronics", "Category")
            .FilterBy("UpdatedName", "Name");

        var json = Utility.Serialize(request);
        var deserialized = Utility.DeserializeObject<PageRequest>(json);

        Assert.That(deserialized, Is.Not.Null);
        var filters = deserialized!.GetFilters().ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(deserialized.CurrentPage, Is.EqualTo(1));
            Assert.That(deserialized.PageSize, Is.EqualTo(10));
            Assert.That(filters, Has.Count.EqualTo(2));
            Assert.That(deserialized.GetFilterValue("Name"), Is.EqualTo("UpdatedName"));
            Assert.That(deserialized.GetFilterValue("Category"), Is.EqualTo("Electronics"));
        }
    }

    [Test]
    public void SerializeDeserialize_ComplexPageRequest_ProducesIdenticalODataQuery()
    {
        var original = new PageRequest(2, 25)
            .AddField(typeof(string), "Name")
            .AddField(typeof(int), "Age")
            .AddField(typeof(decimal?), "Price")
            .Add(new FilterBy("Alice", "Name"))
            .Add(new FilterBy(new AdvancedFilterContext(ODataOperator.GreaterThan, 21), "Age", "21"))
            .Add(new FilterBy(new AdvancedFilterContext(ODataOperator.Between, 10m, 99.99m), "Price"))
            .OrderBy("Name", SortDirection.Ascending)
            .OrderBy("Age", SortDirection.Descending);

        var expectedOData = new ODataPageRequestBuilder(original)
            .UseFilters()
            .UseSort()
            .UsePagination()
            .GetBuilder()
            .ToString();

        var json = Utility.Serialize(original, prettyPrint: true);
        var deserialized = Utility.DeserializeObject<PageRequest>(json);

        Assert.That(deserialized, Is.Not.Null);

        var actualOData = new ODataPageRequestBuilder(deserialized!)
            .UseFilters()
            .UseSort()
            .UsePagination()
            .GetBuilder()
            .ToString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actualOData, Is.EqualTo(expectedOData));
            Assert.That(deserialized.GetHashCode(), Is.EqualTo(original.GetHashCode()));
        }
    }

    [Test]
    public void Deserialize_NonGeneric_ComplexPageRequest_ReconstructsOperations()
    {
        var guid = Guid.NewGuid();
        var request = new PageRequest(3, 40)
            .FilterBy(guid.ToString(), "UserId")
            .FilterBy("05/01/2026", "StartDate")
            .OrderBy("StartDate", SortDirection.Descending);

        var json = Utility.Serialize(request);
        var obj = Utility.Deserialize(typeof(PageRequest), json);

        Assert.That(obj, Is.InstanceOf<PageRequest>());
        var deserialized = (PageRequest)obj!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deserialized.CurrentPage, Is.EqualTo(1));
            Assert.That(deserialized.PageSize, Is.EqualTo(40));
            Assert.That(deserialized.TryGetFilter<Guid>("UserId", out var parsedGuid), Is.True);
            Assert.That(parsedGuid, Is.EqualTo(guid));
            Assert.That(deserialized.TryGetFilter<DateTime>("StartDate", out var parsedDate), Is.True);
            Assert.That(parsedDate, Is.EqualTo(new DateTime(2026, 5, 1)));
            Assert.That(deserialized.GetSortFields().Single().Direction, Is.EqualTo(SortDirection.Descending));
        }
    }

    [Test]
    public void SerializeDeserializeStream_ComplexPageRequest_ReconstructsFromStream()
    {
        var request = new PageRequest(2, 30)
            .FilterBy("Pending", "OrderStatus")
            .FilterBy("150.75", "MinTotal")
            .OrderBy("OrderDate", SortDirection.Descending);

        using var stream = new MemoryStream();
        var serializeResult = Utility.SerializeToStream(request, stream, prettyPrint: true);
        Assert.That(serializeResult.Success, Is.True);

        var jsonBytes = stream.ToArray();
        using var readStream = new MemoryStream(jsonBytes);
        var deserializeResult = Utility.DeserializeObject<PageRequest>(readStream);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deserializeResult.Success, Is.True);
            Assert.That(deserializeResult.Model, Is.Not.Null);
            Assert.That(deserializeResult.Model!.CurrentPage, Is.EqualTo(1));
            Assert.That(deserializeResult.Model.PageSize, Is.EqualTo(30));
            Assert.That(deserializeResult.Model.GetFilterValue("OrderStatus"), Is.EqualTo("Pending"));
            Assert.That(deserializeResult.Model.TryGetFilter<decimal>("MinTotal", out var minTotal), Is.True);
            Assert.That(minTotal, Is.EqualTo(150.75m));
            Assert.That(deserializeResult.Model.GetSortFields().First().Field, Is.EqualTo("OrderDate"));
        }
    }

    [Test]
    public void Serialize_PageRequest_DeserializeAsPageResult_PreservesRequestStateAndOperations()
    {
        var request = new PageRequest(1, 25)
            .AddField(typeof(string), "Name", "text")
            .AddField(typeof(int), "Id", "number")
            .Add(new FilterBy("Alice", "Name"))
            .Add(new FilterBy(new AdvancedFilterContext(ODataOperator.GreaterThan, 10), "Id", "10"))
            .OrderBy("Name", SortDirection.Ascending);
        request.CurrentPage = 3;

        var json = Utility.Serialize(request, prettyPrint: true);
        var pageResult = Utility.DeserializeObject<PageResult<TestModel>>(json);

        Assert.That(pageResult, Is.Not.Null);
        var availableFields = pageResult!.GetAvailableFields().ToList();
        var filters = pageResult.GetFilters().ToList();
        var sorts = pageResult.GetSortFields().ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pageResult.CurrentPage, Is.EqualTo(3));
            Assert.That(pageResult.PageSize, Is.EqualTo(25));
            Assert.That(pageResult.Items, Is.Not.Null);
            Assert.That(pageResult.Items, Is.Empty);
            Assert.That(pageResult.TotalCount, Is.EqualTo(0));
            Assert.That(pageResult.PageCount, Is.EqualTo(0));
            Assert.That(availableFields, Has.Count.EqualTo(2));
            Assert.That(filters, Has.Count.EqualTo(2));
            Assert.That(pageResult.GetFilterValue("Name"), Is.EqualTo("Alice"));
            Assert.That(pageResult.TryGetFilter<int>("Id", out var id), Is.True);
            Assert.That(id, Is.EqualTo(10));
            Assert.That(sorts, Has.Count.EqualTo(1));
            Assert.That(sorts[0].Field, Is.EqualTo("Name"));
            Assert.That(sorts[0].Direction, Is.EqualTo(SortDirection.Ascending));
        }
    }

    [Test]
    public void ServiceSimulation_ReceivePageRequestAndRespondWithPageResult_PreservesDataAndOperations()
    {
        // 1. Client builds and serializes a PageRequest
        var clientRequest = new PageRequest(1, 2)
            .AddField(typeof(string), "Name", "text")
            .FilterBy("Item", "Name")
            .OrderBy("Id", SortDirection.Ascending);
        clientRequest.CurrentPage = 2;

        var requestPayload = Utility.Serialize(clientRequest);

        // 2. Service deserializes the incoming PageRequest, processes query, and creates PageResult via request.Result(...)
        var serverRequest = Utility.DeserializeObject<PageRequest>(requestPayload);
        Assert.That(serverRequest, Is.Not.Null);

        var allData = new List<TestModel>
        {
            new() { Id = 1, Name = "Item 1", Created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, Name = "Item 2", Created = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 3, Name = "Item 3", Created = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 4, Name = "Item 4", Created = new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 5, Name = "Other", Created = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc) }
        };

        var nameFilter = serverRequest!.GetFilterValue("Name") ?? string.Empty;
        var filtered = allData.Where(x => x.Name != null && x.Name.Contains(nameFilter)).ToList();
        var pagedItems = filtered
            .Skip((serverRequest.CurrentPage - 1) * serverRequest.PageSize)
            .Take(serverRequest.PageSize)
            .ToList();

        var serverResult = serverRequest.Result(pagedItems, filtered.Count);
        var responsePayload = Utility.Serialize(serverResult, prettyPrint: true);

        // 3. Client deserializes the response as PageResult<TestModel>
        var clientResult = Utility.DeserializeObject<PageResult<TestModel>>(responsePayload);

        Assert.That(clientResult, Is.Not.Null);
        var items = clientResult!.Items.ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(clientResult.Success, Is.True);
            Assert.That(clientResult.ResultType, Is.EqualTo(CoreOne.Results.ResultType.Success));
            Assert.That(clientResult.CurrentPage, Is.EqualTo(2));
            Assert.That(clientResult.PageSize, Is.EqualTo(2));
            Assert.That(clientResult.TotalCount, Is.EqualTo(4));
            Assert.That(clientResult.PageCount, Is.EqualTo(2));
            Assert.That(items, Has.Count.EqualTo(2));
            Assert.That(items[0].Id, Is.EqualTo(3));
            Assert.That(items[0].Name, Is.EqualTo("Item 3"));
            Assert.That(items[1].Id, Is.EqualTo(4));
            Assert.That(items[1].Name, Is.EqualTo("Item 4"));
            Assert.That(clientResult.GetFilterValue("Name"), Is.EqualTo("Item"));
            Assert.That(clientResult.GetAvailableFields().Single().Field, Is.EqualTo("Name"));
            Assert.That(clientResult.GetSortFields().Single().Field, Is.EqualTo("Id"));
            Assert.That(clientResult.GetSortFields().Single().Direction, Is.EqualTo(SortDirection.Ascending));
        }
    }

    [Test]
    public void ServiceSimulation_StreamRequestToPageResultResponse_RoundTripsOverStreams()
    {
        var clientRequest = new PageRequest(1, 10)
            .Add(new FilterBy(new AdvancedFilterContext(ODataOperator.Between, 1, 50), "Id", "1-50"))
            .OrderBy("Created", SortDirection.Descending, single: true);

        using var requestStream = new MemoryStream();
        var requestSerialize = Utility.SerializeToStream(clientRequest, requestStream);
        Assert.That(requestSerialize.Success, Is.True);

        using var serverReadStream = new MemoryStream(requestStream.ToArray());
        var serverRequestResult = Utility.DeserializeObject<PageRequest>(serverReadStream);
        Assert.That(serverRequestResult.Success, Is.True);
        Assert.That(serverRequestResult.Model, Is.Not.Null);

        var responseItems = new[]
        {
            new TestModel { Id = 10, Name = "Ten", Created = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc) },
            new TestModel { Id = 20, Name = "Twenty", Created = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc) }
        };
        var serverPageResult = serverRequestResult.Model!.Result(responseItems, total: 25);

        using var responseStream = new MemoryStream();
        var responseSerialize = Utility.SerializeToStream(serverPageResult, responseStream);
        Assert.That(responseSerialize.Success, Is.True);

        using var clientReadStream = new MemoryStream(responseStream.ToArray());
        var clientResult = Utility.DeserializeObject<PageResult<TestModel>>(clientReadStream);

        Assert.That(clientResult.Success, Is.True);
        Assert.That(clientResult.Model, Is.Not.Null);
        var pageResult = clientResult.Model!;
        var filter = pageResult.GetFilters().Single();
        var sort = pageResult.GetSortFields().Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pageResult.Success, Is.True);
            Assert.That(pageResult.CurrentPage, Is.EqualTo(1));
            Assert.That(pageResult.PageSize, Is.EqualTo(10));
            Assert.That(pageResult.TotalCount, Is.EqualTo(25));
            Assert.That(pageResult.PageCount, Is.EqualTo(3));
            Assert.That(pageResult.Items, Has.Count.EqualTo(2));
            Assert.That(filter.Field, Is.EqualTo("Id"));
            Assert.That(filter.AdvancedSearch, Is.Not.Null);
            Assert.That(filter.AdvancedSearch!.Operator, Is.EqualTo(ODataOperator.Between));
            Assert.That(sort, Is.TypeOf<SingleOrderBy>());
            Assert.That(sort.Field, Is.EqualTo("Created"));
            Assert.That(sort.Direction, Is.EqualTo(SortDirection.Descending));
        }
    }

    [Test]
    public void ServiceSimulation_FailedPageResultResponse_PreservesResultTypeAndRequestOperations()
    {
        var clientRequest = new PageRequest(1, 20)
            .FilterBy("InvalidQuery", "Name");

        var requestJson = Utility.Serialize(clientRequest);
        var serverRequest = Utility.DeserializeObject<PageRequest>(requestJson);
        Assert.That(serverRequest, Is.Not.Null);

        var failedServerResult = serverRequest!.Result<TestModel>(null, 0, CoreOne.Results.ResultType.Fail);
        var responseJson = Utility.Serialize(failedServerResult);

        var clientResult = Utility.DeserializeObject<PageResult<TestModel>>(responseJson);

        Assert.That(clientResult, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(clientResult!.Success, Is.False);
            Assert.That(clientResult.ResultType, Is.EqualTo(CoreOne.Results.ResultType.Fail));
            Assert.That(clientResult.Items, Is.Empty);
            Assert.That(clientResult.TotalCount, Is.EqualTo(0));
            Assert.That(clientResult.GetFilterValue("Name"), Is.EqualTo("InvalidQuery"));
        }
    }
}