using CoreOne.Operations;

namespace CoreOne.Attributes;

/// <summary>
/// Provides conditional validation based on related property value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RequiredIfAttribute"/> class.
/// </remarks>
/// <param name="propertyName">The other property.</param>
/// <param name="targetValue">Equals this value.</param>
/// <param name="comparisonType">Comparison type</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class RequiredIfAttribute(string propertyName, object? targetValue, ComparisonType comparisonType = ComparisonType.EqualTo) : ValidationAttribute("'{0}' is required because '{1}' has a value {3} '{2}'.")
{
    #region Properties

    /// <summary>
    /// Comparison type
    /// </summary>
    public ComparisonType ComparisonType { get; } = comparisonType;
    /// <summary>
    /// Gets or sets other properties that should be observed for change during validation
    /// </summary>
    /// <value>
    /// Other properties separated by commas (CSV)
    /// </value>
    public string? PingPropertiesOnChange { get; set; }
    /// <summary>
    /// Gets or sets the other property name that will be used during validation.
    /// </summary>
    /// <value>
    /// The other property name.
    /// </value>
    public string PropertyName { get; init; } = propertyName;
    /// <summary>
    /// Gets a value that indicates whether the attribute requires validation context.
    /// </summary>
    /// <returns><c>true</c> if the attribute requires validation context; otherwise, <c>false</c>.</returns>
    public override bool RequiresValidationContext => true;
    /// <summary>
    /// Gets or sets the other property value that will be relevant for validation.
    /// </summary>
    /// <value>
    /// The other property value.
    /// </value>
    public object? TargetValue { get; init; } = targetValue;

    #endregion Properties

    private Metadata TargetMetadata = Metadata.Empty;

    /// <summary>
    /// Applies formatting to an error message, based on the data field where the error occurred.
    /// </summary>
    /// <param name="name">The name to include in the formatted message.</param>
    /// <returns>
    /// An instance of the formatted error message.
    /// </returns>
    public override string FormatErrorMessage(string name)
    {
        return string.Format(
            CultureInfo.CurrentCulture,
            ErrorMessageString,
            name,
            PropertyName,
            TargetValue,
            ComparisonType);
    }

    /// <summary>
    /// Indicates whether the current state is required for the property
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public virtual bool IsRequired(object? model)
    {
        InitializeMetadata(model);
        object? otherValue = TargetMetadata.GetValue(model);
        // check if this value is actually required and validate it
        return otherValue.CompareToObject(TargetValue, ComparisonType);
    }

    /// <summary>
    /// Validates the specified value with respect to the current validation attribute.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">The context information about the validation operation.</param>
    /// <returns>
    /// An instance of the <see cref="T:System.ComponentModel.DataAnnotations.ValidationResult" /> class.
    /// </returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var model = validationContext.ObjectInstance;
        return IsRequired(model) && (value is null || string.IsNullOrEmpty(value?.ToString()))
            ? new ValidationResult(FormatErrorMessage(validationContext.MemberName ?? ""))
            : ValidationResult.Success;
    }

    private void InitializeMetadata(object? model)
    {
        if (TargetMetadata == Metadata.Empty)
        {
            var type = model?.GetType();
            TargetMetadata = MetaType.GetMetadata(type, PropertyName);
            if (TargetMetadata == Metadata.Empty)
                throw new NotSupportedException($"Property {PropertyName} does not exist in type {type}");
        }
    }
}