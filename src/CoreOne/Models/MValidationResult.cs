namespace CoreOne.Models;

public class MValidationResult : IResult
{
    public static readonly MValidationResult MSuccess = new();
    public IReadOnlyList<string>? ErrorMessages { get; init; }
    public bool IsValid { get; init; }
    public string? Message { get; private set; }
    public ResultType ResultType { get; private set; }
    public bool Success => ResultType == ResultType.Success;

    public MValidationResult()
    {
        IsValid = true;
        ErrorMessages = null;
    }

    public MValidationResult(string? errorMsg)
    {
        ErrorMessages = string.IsNullOrEmpty(errorMsg) ? null : new List<string> { errorMsg! };
        IsValid = ErrorMessages is null || ErrorMessages.Count == 0;
        InitializeProperties();
    }

    public MValidationResult(IEnumerable<string>? errorMessages)
    {
        ErrorMessages = errorMessages?.ToList();
        IsValid = ErrorMessages is null || ErrorMessages.Count == 0;
        InitializeProperties();
    }

    public static implicit operator bool(MValidationResult? result) => result?.IsValid == true;

    public override bool Equals(object? obj) => obj is MValidationResult result && IsValid == result.IsValid &&
        ((ErrorMessages is null && result.ErrorMessages is null) ||
        (ErrorMessages is not null && result.ErrorMessages is not null && ErrorMessages.SequenceEqual(result.ErrorMessages)));

    public string? GetErrorMessages() => ErrorMessages?.Count > 0 ? string.Join(". ", ErrorMessages) : null;

    public override int GetHashCode() => (ErrorMessages, IsValid).GetHashCode();

    public IResult ToResult() => IsValid ? Result.Ok : Result.Fail(GetErrorMessages());

    private void InitializeProperties()
    {
        ResultType = IsValid ? ResultType.Success : ResultType.Fail;
        Message = GetErrorMessages();
    }
}