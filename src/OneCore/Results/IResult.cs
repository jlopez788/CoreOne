namespace CoreOne.Results;

public interface IResult
{
    string? Message { get; }
    ResultType ResultType { get; }
    public bool Success => ResultType == ResultType.Success;
}

public interface IResult<TModel> : IResult
{ 
    TModel? Model { get; }
}

public interface IResult<TModel, TStatus> : IResult<TModel>, IStatusResult<TStatus>
{ }