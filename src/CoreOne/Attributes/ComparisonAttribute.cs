using CoreOne.Operations;

namespace CoreOne.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class ComparisonAttribute(string comparisonProperty, ComparisonType comparisonType) : ValidationAttribute
{
    private readonly string ComparisonProperty = comparisonProperty;
    private readonly ComparisonType ComparisonType = comparisonType;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var comparable = typeof(IComparable);
        var sourceValue = value as IComparable;
        ErrorMessage = ErrorMessageString;

        if (value is not null && !comparable.IsAssignableFrom(value.GetType()))
        {
            throw new ArgumentException("value has not implemented IComparable interface");
        }

        var property = validationContext.ObjectType.GetProperty(ComparisonProperty) ?? throw new ArgumentException("Comparison property with this name not found");
        var isSVNull = sourceValue == null;
        var targetValue = property.GetValue(validationContext.ObjectInstance);
        var isTVNull = targetValue == null;

        if (!isSVNull && !isTVNull && !Equals(value?.GetType(), targetValue?.GetType()))
        {
            throw new ArgumentException("The properties types must be the same");
        }

        var isValid = sourceValue.CompareToObject(targetValue, ComparisonType);
        return isValid ? ValidationResult.Success : new ValidationResult(ErrorMessage);
    }
}