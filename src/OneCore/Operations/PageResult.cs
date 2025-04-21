namespace CoreOne.Operations;

public class PageResult<T> : PageRequest, IResult<ICollection<T>>
{
    public ICollection<T>? Results { get; set; }
    public int PageCount { get; set; }
    public int TotalCount { get; set; }
    public ResultType ResultType { get; set; }
    public ICollection<T>? Model => Results;
    public string? Message { get; }
    public bool Success => ResultType == ResultType.Success;

    public PageResult(int pageSize)
    {
        CurrentPage = 1;
        PageSize = pageSize;
        Results = [];
    }

    public PageResult(IEnumerable<T>? data, int page, int pageSize, int total)
    {
        ResultType = ResultType.Success;
        Results = data is not null ? new List<T>(data) : new List<T>(10);
        CurrentPage = page;
        PageSize = pageSize;
        TotalCount = total;
        PageCount = total < pageSize || pageSize == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
    }
} 