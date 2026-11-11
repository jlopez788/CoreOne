# CoreOne Operations, Pagination & OData Builders (`CoreOne.Operations`, `CoreOne.ODataBuilders`, `CoreOne.Extensions`)

CoreOne provides a composable operation pipeline (`IOperation`, `PageRequest`, `CursorRequest`) for filtering, sorting, and paging data—both against in-memory/EF `IQueryable<T>` sources and translated into OData query strings via `ODataBuilder` and `ODataPageRequestBuilder`.

---

## 1. Operations Pipeline (`CoreOne.Operations`)

### `BaseOperationRequest<T>` & `IOperation`
Both `PageRequest` and `CursorRequest` inherit from `BaseOperationRequest<T>` and maintain a `List<IOperation> Operations` plus a lifecycle `SToken Token` (which can be cancelled and renewed via `CancelCurrentTokenThenRenew()` when filters change).

| Operation Type | Purpose | Usage on Request |
|---|---|---|
| `AvailableField(Type type, string field, string? format = null)` | Registers a typed, filterable field metadata entry on the request. | `request.AddField(typeof(string), "Email")` |
| `FilterBy(string value, string? field = null)`<br>`FilterBy(AdvancedFilterContext? search, string? field, string? value = null)` | Adds a field-specific or global (`field == null`) filter value, or an advanced operator filter. | `request.FilterBy("active", "Status")`<br>`request.FilterBy("john")` (global search) |
| `ResetFilter(string? field)` | Removes existing `FilterBy` operations matching `field` (or all if `field` is null). | `request.Add(new ResetFilter("Status"))` |
| `MergeFilter(string? field, Func<FilterBy?, FilterBy> onmerge)` | Atomically merges or replaces an existing `FilterBy` for `field`. | `request.Add(new MergeFilter("Status", cur => ...))` |
| `OrderBy(string field, SortDirection direction)` | Appends a sort clause (`SortDirection.Ascending` or `Descending`). | `request.OrderBy("CreatedAt", SortDirection.Descending)` |
| `SingleOrderBy(string field, SortDirection direction)` | Clears previous sort operations and sets a single active sort field. | `request.OrderBy("Name", SortDirection.Ascending, single: true)` |
| `ClearOrderBy.Default` | Removes all `OrderBy` / `SingleOrderBy` operations from `Operations`. | `request.Add(ClearOrderBy.Default)` |

### Inspecting Operations on a Request
```csharp
IEnumerable<AvailableField> fields = request.GetAvailableFields();
IEnumerable<FilterBy> filters      = request.GetFilters();
IEnumerable<OrderBy> sorts         = request.GetSortFields();
string? statusFilter               = request.GetFilterValue("Status");
bool hasMinAge                     = request.TryGetFilter<int>("MinAge", out int minAge);
```

---

## 2. Page-Based & Cursor-Based Pagination

### `PageRequest` & `PageResult<T>` (`ICollectionResult<T>`)
- **`PageRequest(int currentPage, int pageSize)`**:
  - Properties: `int CurrentPage`, `int PageSize`.
  - When a filter is modified (`OnFilterChanged()`), `CurrentPage` automatically resets to `1`.
  - Factory method: `PageResult<T> pageResult = request.Result(items, totalCount, ResultType.Success);`
- **`PageResult<T> : PageRequest, ICollectionResult<T>`**:
  - Properties: `ICollection<T> Items`, `int TotalCount`, `int PageCount`, `ResultType ResultType`, `bool Success`, `string? Message`.

### `CursorRequest`, `CursorResult<T>` & `CursorResultExtensions`
- **`CursorRequest(string? cursor = null)`**:
  - Properties: `string? Cursor`.
  - Factory method: `CursorResult<T> cursorResult = request.Result(items, nextCursor);`
- **`CursorResult<T> : BaseOperationRequest<CursorResult<T>>, ICollectionResult<T>`**:
  - Properties: `ICollection<T>? Items`, `string? NextCursor`, `bool HasNextCursor` (`!string.IsNullOrEmpty(NextCursor)`), `ResultType ResultType`, `bool Success`.
  - Sync mapping: `CursorResult<TNext> mapped = cursorResult.Select(item => ToDto(item));`
- **Async Fluent Extensions (`CursorResultExtensions`)**:
  ```csharp
  CursorResult<UserDto> result = await FetchUsersCursorAsync(request)
      .SelectAsync(user => ToDto(user))
      .OnSuccessAsync((items, nextCursor) => Logger.LogInformation("Loaded {Count}, next: {Cursor}", items.Count, nextCursor));
  ```

---

## 3. `IQueryable<T>` Extensions (`QueryExtensions`)

Apply `OrderBy` and `PageRequest` directly to EF Core or LINQ `IQueryable<T>` queries:
```csharp
IQueryable<User> query = dbContext.Users
    .OrderBy(pageRequest.GetSortFields()) // Applies OrderBy / ThenBy / OrderByDescending / ThenByDescending dynamically
    .Paginate(pageRequest);               // Applies .Skip((CurrentPage - 1) * PageSize).Take(PageSize)

// Or sort by string expression (e.g. "LastName desc, FirstName asc")
IQueryable<User> sorted = dbContext.Users.OrderBy("LastName desc, FirstName asc");
```

---

## 4. OData Query Building (`CoreOne.ODataBuilders`)

### `ODataBuilder`
Fluent builder for constructing OData query strings (`$select`, `$expand`, `$filter`, `$orderby`, `$top`, `$skip`, `$count`):
```csharp
var builder = new ODataBuilder()
    .Url("api/users")
    .Select("Id,Name,Email")
    .Expand("Roles")
    .FilterBy(ODataBuilder.AND, $"IsActive eq {true} and CreatedAt ge {startDate:yyyy-MM-dd}")
    .OrderBy(SortDirection.Descending, "CreatedAt")
    .Paginate(pageRequest) // Sets $top and $skip (and $count=true) from PageRequest
    .Count();

string queryUrl = builder.ToString(); // Implicit string conversion also supported: string? url = builder;
```
- **FormattableString Interpolation**: `FilterBy(string op, FormattableString value)` automatically wraps interpolated arguments in `ODataArgument` and formats them using `Utility.AsODataUrlValue` (quoting strings, formatting dates/GUIDs/enums/booleans according to OData rules).
- **`FilterSegment`**: Group nested `And` / `Or` filter clauses or build `in` / multi-value clauses via `FilterSegment.Create(items, BinaryOperator.Or, "Status")`.

### `ODataPageRequestBuilder` & `ODataOperator`
Translates a `PageRequest` (its `AvailableField` metadata, `FilterBy` criteria, `AdvancedFilterContext`, `OrderBy`, and paging) into an `ODataBuilder`:
```csharp
ODataBuilder odata = new ODataPageRequestBuilder(pageRequest)
    .UsePagination()
    .UseSort(defaultSort: OrderBy.Descending("CreatedAt"))
    .UseFilters()
    .UseInterceptOnField("CustomField", (ctx, next) =>
        new FilterSegment(BinaryOperator.And, $"CustomCode eq {ctx.FilterResult.Value}"))
    .GetBuilder();
```
- **Built-in `ODataOperator` Lookup Types**:
  - `ODataOperator.Contains`, `Equal`, `GreaterThan`, `LessThan`, `Between`, `InArray`, `StartsWith`, `NullOrEmpty`, `NotNull`.
- **Custom Type Handlers (`FilterTypeHandlerFactory`)**:
  - Register custom `IFilterTypeHandler` via `.UseHandler(new FilterTypeHandler(typeof(MyType), parseFn, createSegmentFn))` or globally on `FilterTypeHandlerFactory.Default`.
