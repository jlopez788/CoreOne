namespace CoreOne.Operations;

public class CursorRequest : BaseOperationRequest<CursorRequest>
{
    public string? Cursor { get; set; }

    public CursorRequest()
    {
        Operations = [];
        Token = SToken.Create();
    }

    public CursorRequest(string? cursor)
    {
        Cursor = cursor;
        Token = SToken.Create();
        Operations = [];
    }

    protected override object GetHashCodeData() => Cursor ?? string.Empty;

    protected override void OnFilterChanged()
    {
        Cursor = null;
    }

    public CursorResult<T> Result<T>(IEnumerable<T>? models, string? cursor, ResultType resultType = ResultType.Success)
    {
        var result = new CursorResult<T>(models, cursor, Token) {
            ResultType = resultType
        };
        result.Operations.AddRange(Operations);
        return result;
    }
}