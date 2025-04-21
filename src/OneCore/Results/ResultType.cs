namespace CoreOne.Results;

[Flags]
public enum ResultType
{
    None = 0x10,
    Success = 0x01,
    Fail = 0x02,
    Exception = 0x04,
    Cancelled = 0x08
}