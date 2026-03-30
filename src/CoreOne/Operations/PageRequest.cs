namespace CoreOne.Operations;

public class PageRequest : BaseOperationRequest<PageRequest>
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }

    public PageRequest()
    { }

    public PageRequest(int currentPage, int pageSize)
    {
        CurrentPage = currentPage;
        PageSize = pageSize;
    }

    public PageResult<T> Result<T>(IEnumerable<T>? models, int total, ResultType resultType = ResultType.Success)
    {
        var result = new PageResult<T>(models, CurrentPage, PageSize, total) {
            Token = Token,
            ResultType = resultType
        };
        result.Operations.AddRange(Operations);
        return result;
    }

    protected override object GetHashCodeData() => new {
        PageSize,
        CurrentPage
    };

    protected override void OnFilterChanged()
    {
        CurrentPage = 1;
    }
}