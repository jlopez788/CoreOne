namespace CoreOne.Results;

public interface IResult
{
    string? Message { get; }
    ResultType ResultType { get; }
#if NET9_0_OR_GREATER
    public bool Success => ResultType == ResultType.Success;
#else
    public bool Success { get; }// => ResultType == ResultType.Success;
#endif
}

public interface IResult<TModel> : IResult
{
    TModel? Model { get; }
}

public interface IResult<TModel, TStatus> : IResult<TModel>, IStatusResult<TStatus>
{ }