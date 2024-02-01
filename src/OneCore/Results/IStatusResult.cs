namespace OneCore.Results;

public interface IStatusResult<TStatus> : IResult
{
    bool IsSuccessStatusCode { get; }
    TStatus StatusCode { get; }
}