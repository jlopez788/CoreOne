using System.Globalization;

namespace CoreOne.Attributes;

/// <summary>
/// Provides conditional validation based on related property value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RequiredIfAttribute"/> class.
/// </remarks>
/// <param name="otherProperty">The other property.</param>
/// <param name="equalsValue">Equals this value.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class RequiredIfAttribute(string otherProperty, object? equalsValue) : ValidationAttribute("'{0}' is required because '{1}' has a value {3}'{2}'.")
{
    #region Properties

    /// <summary>
    /// Gets or sets a value indicating whether other property's value should match or differ from provided other property's value (default is <c>false</c>).
    /// </summary>
    /// <value>
    ///   <c>true</c> if other property's value validation should be inverted; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// How this works
    /// - true: validated property is required when other property doesn't equal provided value
    /// - false: validated property is required when other property matches provided value
    /// </remarks>
    public bool IsInverted { get; set; } = false;

    /// <summary>
    /// Gets or sets the other property name that will be used during validation.
    /// </summary>
    /// <value>
    /// The other property name.
    /// </value>
    public string OtherProperty { get; private set; } = otherProperty;

    /// <summary>
    /// Gets or sets the display name of the other property.
    /// </summary>
    /// <value>
    /// The display name of the other property.
    /// </value>
    public string? OtherPropertyDisplayName { get; set; }

    /// <summary>
    /// Gets or sets the other property value that will be relevant for validation.
    /// </summary>
    /// <value>
    /// The other property value.
    /// </value>
    public object? OtherPropertyValue { get; private set; } = equalsValue;

    /// <summary>
    /// Gets or sets other properties that should be observed for change during validation
    /// </summary>
    /// <value>
    /// Other properties separated by commas (CSV)
    /// </value>
    public string? PingPropertiesOnChange { get; set; }

    /// <summary>
    /// Gets a value that indicates whether the attribute requires validation context.
    /// </summary>
    /// <returns><c>true</c> if the attribute requires validation context; otherwise, <c>false</c>.</returns>
    public override bool RequiresValidationContext => true;

    #endregion Properties

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
            OtherPropertyDisplayName ?? OtherProperty,
            OtherPropertyValue,
            IsInverted ? "other than " : "of ");
    }

    public virtual bool IsRequired(object? target)
    {
        var otherProperty = target?.GetType()?.GetProperty(OtherProperty);
        if (otherProperty == null)
            return false;
        object? otherValue = otherProperty?.GetValue(target);
        // check if this value is actually required and validate it
        return (!IsInverted && Equals(otherValue, OtherPropertyValue)) || (IsInverted && !Equals(otherValue, OtherPropertyValue));
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
        ArgumentNullException.ThrowIfNull(validationContext);

        var otherProperty = validationContext.ObjectType.GetProperty(OtherProperty);
        if (otherProperty == null)
            return new ValidationResult($"Validation: Could not find a property named '{OtherProperty}'.");

        object? otherValue = otherProperty.GetValue(validationContext.ObjectInstance);
        if (otherValue is null && OtherPropertyValue is null)
            return ValidationResult.Success;
        if ((otherValue is null && OtherPropertyValue is not null))
            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

        // check if this value is actually required and validate it
        if ((!IsInverted && Equals(otherValue, OtherPropertyValue)) || (IsInverted && !Equals(otherValue, OtherPropertyValue)))
        {
            if (value == null)
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));

            // additional check for strings so they're not empty
            if (value is string val && string.IsNullOrWhiteSpace(val))
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }

        return ValidationResult.Success;
    }
}