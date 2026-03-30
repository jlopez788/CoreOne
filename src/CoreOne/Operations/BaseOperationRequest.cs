namespace CoreOne.Operations;

public abstract class BaseOperationRequest<T> where T : BaseOperationRequest<T>
{
    public static readonly Type AVAILABLE = typeof(AvailableField);
    public static readonly Type FILTER = typeof(FilterBy);
    public static readonly Type ORDER = typeof(OrderBy);

    public List<IOperation> Operations { get; protected set; } = [];
    public SToken Token { get; protected set; }

    protected BaseOperationRequest()
    {
        Token = SToken.Create();
    }

    protected BaseOperationRequest(SToken? token) => Token = token ?? SToken.Create();

    public T Add(IOperation? operation)
    {
        if (operation != null)
            Operations.Add(operation);
        return (T)this;
    }

    public T AddField(Type type, string field, string? format = null)
    {
        if (!string.IsNullOrWhiteSpace(field))
            Set(new AvailableField(type, field, format));
        return (T)this;
    }

    public T ApplyOperation(IOperation operation)
    {
        if (operation is ResetFilter reset)
        {
            ClearOperations(p => p is FilterBy fb && fb.Field.Matches(reset.Field));
            OnFilterChanged();
        }
        else if (operation is FilterBy filter)
        {
            var target = Operations.OfType<FilterBy>().FirstOrDefault(p => p.Field.Matches(filter.Field)) ?? filter;
            target.AdvancedSearch = filter.AdvancedSearch;
            ApplyFilter(target);
            OnFilterChanged();
        }
        else if (operation is MergeFilter merge)
        {
            var current = Operations.OfType<FilterBy>().FirstOrDefault(p => p.Field.Matches(merge.Field));
            current = merge.Merge(current);
            ApplyFilter(current);
            OnFilterChanged();
        }

        return (T)this;

        void ApplyFilter(FilterBy filter)
        {
            var next = Operations.ToList().AddOrUpdate(filter, p => p is FilterBy fb && fb.Field.Matches(filter.Field));
            Operations.Clear();
            Operations.AddRange(next);
        }
    }

    public void CancelCurrentTokenThenRenew()
    {
        Token.Cancel();
        Token = SToken.Create();
    }

    public T ClearOperations(Predicate<IOperation> predicate)
    {
        if (predicate is not null)
            Operations.RemoveAll(predicate);
        return (T)this;
    }

    public T FilterBy(string? value, string? field = null)
    {
        field ??= string.Empty;
        OnFilterChanged(); // When changing filter reset back to first page
        if (!string.IsNullOrEmpty(value))
        {
            FilterBy? filter = null;
            if (!string.IsNullOrWhiteSpace(field))
            {
                var matching = Operations.OfType<FilterBy>()
                       .FirstOrDefault(p => p.Field == field);
                //if (matching?.AdvancedSearch is not null)
                //{
                //    filter = new FilterBy(value, field) {
                //        AdvancedSearch = matching.AdvancedSearch.Copy()
                //    };
                //}
            }
            filter ??= new FilterBy(value!, field);
            return Set(filter);
        }
        return ClearOperations(p => p.Field == field && p.GetType() == FILTER);
    }

    public IEnumerable<AvailableField> GetAvailableFields() => Operations.OfType<AvailableField>();

    public IEnumerable<FilterBy> GetFilters() => Operations.OfType<FilterBy>();

    public string? GetFilterValue(string? field)
    {
        var clean = field ?? string.Empty;
        return GetFilters().FirstOrDefault(p => p.Field.Matches(clean))?.Value;
    }

    public override int GetHashCode()
    {
        using var stream = new MemoryStream();
        var model = new {
            Data = GetHashCodeData(),
            Operations = Operations ?? []
        };
        var result = Utility.SerializeToStream(model, stream);
        if (result.Success)
        {
            stream.Flush();
            var buffer = stream.ToArray();
            return (int)Crc32.Compute(buffer);
        }
        return 0;
    }

    public IEnumerable<OrderBy> GetSortFields() => Operations.OfType<OrderBy>();

    /// <summary>
    /// Sets Order By to request
    /// </summary>
    /// <param name="orderBy">Order By operation</param>
    /// <param name="single">TRUE: Single Order By, FALSE: Multiple Order By</param>
    /// <returns></returns>
    public T OrderBy(OrderBy? orderBy, bool single = false)
    {
        if (orderBy is not null)
        {
            if (single)
                Operations.RemoveAll(p => p.GetType() == ORDER);
            Operations = Operations.AddOrUpdate(orderBy, p => (single || p.Field.Matches(orderBy.Field)) && p.GetType() == ORDER);
        }
        return (T)this;
    }

    public T OrderBy(string field, SortDirection direction) => Set(new OrderBy(field, direction));

    public T UseToken(CancellationToken cancellationToken)
    {
        Token = SToken.CreateLinkedTokens(cancellationToken, Token);
        return (T)this;
    }

    protected abstract object GetHashCodeData();

    protected abstract void OnFilterChanged();

    private T Set<TOperation>(TOperation operation) where TOperation : IOperation
    {
        Operations = Operations.AddOrUpdate(operation, p => p.Field == operation.Field && p.GetType() == typeof(T));
        return (T)this;
    }
}