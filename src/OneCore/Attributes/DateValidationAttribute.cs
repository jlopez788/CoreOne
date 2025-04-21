namespace CoreOne.Attributes;

/// <summary>
/// Date validation, ensure date is > 1900 or not null
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class DateValidationAttribute : ValidationAttribute
{
    public bool AllowNull { get; init; }

    public override bool IsValid(object? value) => (AllowNull && value is null) ||
        (value is not null && value switch {
            DateTime dt => dt.Date != DateTime.MinValue.Date && dt.Year > 1900,
            DateOnly dto => dto != DateOnly.MinValue,
            _ => false
        });
}
