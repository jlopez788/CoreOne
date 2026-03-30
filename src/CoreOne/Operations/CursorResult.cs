namespace CoreOne.Operations;

public class CursorResult<T> : BaseOperationRequest<CursorResult<T>>, ICollectionResult<T>
{
#if NET9_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(NextCursor))]
#endif
    public bool HasNextCursor => !string.IsNullOrEmpty(NextCursor);
    public ICollection<T>? Items { get; }
    public string? Message { get; }
    public string? NextCursor { get; init; }
    public ResultType ResultType { get; set; }
    public bool Success => ResultType == ResultType.Success;

    public CursorResult()
    {
        Items = [];
    }

    public CursorResult(IEnumerable<T>? data, string? cursor = null, SToken? token = null) : base(token)
    {
        ResultType = ResultType.Success;
        Items = data is not null ? [.. data] : new List<T>(10);
        NextCursor = cursor;
    }

    public CursorResult<TNext> Result<TNext>(IEnumerable<TNext>? models, string? cursor, ResultType resultType = ResultType.Success)
    {
        var result = new CursorResult<TNext>(models, cursor, Token) {
            ResultType = resultType
        };
        result.Operations.AddRange(Operations);
        return result;
    }

    protected override object GetHashCodeData() => NextCursor ?? string.Empty;

    protected override void OnFilterChanged()
    { }
}