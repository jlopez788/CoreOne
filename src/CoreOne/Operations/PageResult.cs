namespace CoreOne.Operations;

public class PageResult<T> : PageRequest, IResult<ICollection<T>>
{
    public ICollection<T>? Items { get; set; }
    public int PageCount { get; set; }
    public int TotalCount { get; set; }
    public ResultType ResultType { get; set; }
    public ICollection<T>? Model => Items;
    public string? Message { get; }
    public bool Success => ResultType == ResultType.Success;

    public PageResult(int pageSize)
    {
        CurrentPage = 1;
        PageSize = pageSize;
        Items = [];
    }

    public PageResult(IEnumerable<T>? data, int page, int pageSize, int total)
    {
        ResultType = ResultType.Success;
        Items = data is not null ? [.. data] : new List<T>(10);
        CurrentPage = page;
        PageSize = pageSize;
        TotalCount = total;
        PageCount = total < pageSize || pageSize == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
    }
} 