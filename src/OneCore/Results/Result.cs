namespace OneCore.Results;

public class Result : IResult
{
    public static readonly IResult Ok = new Result(ResultType.Success, string.Empty);

    #region Static

    /// <summary>
    /// Create a failed instance
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static IResult Fail(string? message = null) => new Result(ResultType.Fail, message ?? "Invalid result");

    /// <summary>
    /// Create a failed instance
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static IResult<T> Fail<T>(string? message = null) => new Result<T>(ResultType.Fail, message ?? "Invalid result");

    /// <summary>
    /// Create an execption instance
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    public static IResult FromException(Exception ex)
    {
        var (type, msg) = GetExceptionData(ex);
        return new Result(type, msg);
    }

    /// <summary>
    /// Create an execption instance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ex"></param>
    /// <returns></returns>
    public static IResult<T> FromException<T>(Exception ex)
    {
        var (type, msg) = GetExceptionData(ex);
        return new Result<T>(type, msg);
    }

    internal static (ResultType result, string? message) GetExceptionData(Exception? ex)
    {
        if (ex is TaskCanceledException or OperationCanceledException)
            return (ResultType.Fail, "Task has been cancelled by user/app");
        else if (ex is ObjectDisposedException)
            return (ResultType.Fail, "Object has been disposed. Can no longer use this object");
        else if (ex is NullReferenceException)
            return (ResultType.Exception, "Null reference exception");
        return (ResultType.Exception, ex?.InnerException?.Message ?? ex?.Message);
    }

    #endregion

    public string? Message { get; init; }
    public ResultType ResultType { get; init; }

    public Result() => ResultType = ResultType.Success;

    public Result(ResultType resultType, string? message)
    {
        Message = message;
        ResultType = resultType;
    }
}

public class Result<T> : Result, IResult<T>
{
    public T? Model { get; init; }

    public Result()
    { }

    public Result(ResultType result, string? msg)
    {
        ResultType = result;
        Message = msg;
    }

    public Result(T? model, bool requireInstance, ResultType resultType = ResultType.Success)
    {
        Model = model;
        ResultType = resultType;
        if (requireInstance && model is null)
        {
            ResultType = ResultType.Fail;
            Message = "Model is null. Invalid state";
        }
    }

    public Result(T? model, ResultType resultType = ResultType.Success)
    {
        Model = model;
        ResultType = resultType;
    }

    public static implicit operator Result<T>(T model) => new(model);

    public static implicit operator T?(Result<T> response) => (response is not null) ? response.Model : default;
}