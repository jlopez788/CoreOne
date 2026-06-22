namespace CoreOne.Results;

public class CollectionResult<T> : Result, ICollectionResult<T>
{
    public ICollection<T>? Items { get; init; }

    public CollectionResult() { }

    public CollectionResult(ICollection<T>? items, ResultType resultType = ResultType.Success, string? message = null)
    {
        Items = items;
        ResultType = resultType;
        Message = message;
    }

    public CollectionResult(ResultType resultType, string? message = null)
    {
        ResultType = resultType;
        Message = message;
    }
}