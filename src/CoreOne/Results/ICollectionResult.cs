namespace CoreOne.Results;

public interface ICollectionResult<TModel> : IResult
{
    ICollection<TModel>? Items { get; }
}