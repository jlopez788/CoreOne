using CoreOne.Attributes;
using CoreOne.Models;
using System.ComponentModel.DataAnnotations;

namespace Tests.Attributes;

[TestFixture]
public class RequiredIfAttributeTests
{
    private class TestModel
    {
        public string? Status { get; set; }

        [RequiredIf(nameof(Status), "Active")]
        public string? RequiredField { get; set; }

        [RequiredIf(nameof(Status), "Inactive", ComparisonType.NotEqualTo)]
        public string? RequiredWhenNotInactive { get; set; }
    }

    private class TestModelWithNull
    {
        public string? NullableProperty { get; set; }

        [RequiredIf(nameof(NullableProperty), null)]
        public string? RequiredWhenNull { get; set; }
    }

    [Test]
    public void Constructor_SetsProperties()
    {
        var attribute = new RequiredIfAttribute("Status", "Active");

        Assert.Multiple(() => {
            Assert.That(attribute.PropertyName, Is.EqualTo("Status"));
            Assert.That(attribute.TargetValue, Is.EqualTo("Active"));
            Assert.That(attribute.ComparisonType, Is.EqualTo(ComparisonType.EqualTo));
            Assert.That(attribute.RequiresValidationContext, Is.True);
        });
    }

    [Test]
    public void IsValid_WhenOtherPropertyMatches_AndValueProvided_ReturnsSuccess()
    {
        var model = new TestModel { Status = "Active", RequiredField = "Value" };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.RequiredField) };
        var attribute = new RequiredIfAttribute(nameof(TestModel.Status), "Active");

        var result = attribute.GetValidationResult(model.RequiredField, context);

        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenOtherPropertyMatches_AndValueMissing_ReturnsError()
    {
        var model = new TestModel { Status = "Active", RequiredField = null };
        var context = new ValidationContext(model) {
            MemberName = nameof(TestModel.RequiredField),
            DisplayName = "Required Field"
        };
        var attribute = new RequiredIfAttribute(nameof(TestModel.Status), "Active");

        var result = attribute.GetValidationResult(model.RequiredField, context);

        Assert.Multiple(() => {
            Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
            Assert.That(result?.ErrorMessage, Does.Contain("RequiredField"));
            Assert.That(result?.ErrorMessage, Does.Contain("Status"));
        });
    }

    [Test]
    public void IsValid_WhenOtherPropertyDoesNotMatch_AndValueMissing_ReturnsSuccess()
    {
        var model = new TestModel { Status = "Inactive", RequiredField = null };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.RequiredField) };
        var attribute = new RequiredIfAttribute(nameof(TestModel.Status), "Active");

        var result = attribute.GetValidationResult(model.RequiredField, context);

        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WithEmptyString_ReturnsError()
    {
        var model = new TestModel { Status = "Active", RequiredField = "" };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.RequiredField) };
        var attribute = new RequiredIfAttribute(nameof(TestModel.Status), "Active");

        var result = attribute.GetValidationResult(model.RequiredField, context);

        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WithWhitespace_ReturnsError()
    {
        var model = new TestModel { Status = "Active", RequiredField = "   " };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.RequiredField) };
        var attribute = new RequiredIfAttribute(nameof(TestModel.Status), "Active");

        var result = attribute.GetValidationResult(model.RequiredField, context);

        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WithInverted_WhenNotMatching_RequiresValue()
    {
        var model = new TestModel { Status = "Active", RequiredWhenNotInactive = null };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.RequiredWhenNotInactive) };
        var attribute = new RequiredIfAttribute(nameof(TestModel.Status), "Inactive", ComparisonType.NotEqualTo);

        var result = attribute.GetValidationResult(model.RequiredWhenNotInactive, context);

        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WithInverted_WhenMatching_DoesNotRequireValue()
    {
        var model = new TestModel { Status = "Inactive", RequiredWhenNotInactive = null };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.RequiredWhenNotInactive) };
        var attribute = new RequiredIfAttribute(nameof(TestModel.Status), "Inactive", ComparisonType.NotEqualTo);

        var result = attribute.GetValidationResult(model.RequiredWhenNotInactive, context);

        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenOtherPropertyNotFound_ReturnsError()
    {
        var model = new TestModel { Status = "Active" };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.RequiredField) };
        var attribute = new RequiredIfAttribute("NonExistentProperty", "Value");

        Assert.Throws<NotSupportedException>(() => attribute.GetValidationResult(model.RequiredField, context));
    }

    [Test]
    public void IsValid_WhenBothPropertiesNull_ReturnsRequired()
    {
        var model = new TestModelWithNull { NullableProperty = null, RequiredWhenNull = null };
        var context = new ValidationContext(model) { MemberName = nameof(TestModelWithNull.RequiredWhenNull) };
        var attribute = new RequiredIfAttribute(nameof(TestModelWithNull.NullableProperty), null);

        var result = attribute.GetValidationResult(model.RequiredWhenNull, context);

        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenOtherPropertyIsNull_AndExpectingNull_RequiresValue()
    {
        var model = new TestModelWithNull { NullableProperty = null, RequiredWhenNull = null };
        var context = new ValidationContext(model) { MemberName = nameof(TestModelWithNull.RequiredWhenNull) };
        var attribute = new RequiredIfAttribute(nameof(TestModelWithNull.NullableProperty), null);

        var result = attribute.GetValidationResult(model.RequiredWhenNull, context);

        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
        Assert.That(result.ErrorMessage?.Contains("is required"), Is.True);
    }

    [Test]
    public void IsValid_WhenOtherPropertyHasValue_AndExpectingNull_DoesNotRequireValue()
    {
        var model = new TestModelWithNull { NullableProperty = "SomeValue", RequiredWhenNull = null };
        var context = new ValidationContext(model) { MemberName = nameof(TestModelWithNull.RequiredWhenNull) };
        var attribute = new RequiredIfAttribute(nameof(TestModelWithNull.NullableProperty), null);

        var result = attribute.GetValidationResult(model.RequiredWhenNull, context);

        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsRequired_WhenConditionMatches_ReturnsTrue()
    {
        var model = new TestModel { Status = "Active" };
        var attribute = new RequiredIfAttribute(nameof(TestModel.Status), "Active");

        var result = attribute.IsRequired(model);

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsRequired_WhenConditionDoesNotMatch_ReturnsFalse()
    {
        var model = new TestModel { Status = "Inactive" };
        var attribute = new RequiredIfAttribute(nameof(TestModel.Status), "Active");

        var result = attribute.IsRequired(model);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsRequired_WithNullTarget_Throws()
    {
        var attribute = new RequiredIfAttribute("Status", "Active");
        Assert.Throws<NotSupportedException>(() => attribute.IsRequired(null));
    }

    [Test]
    public void FormatErrorMessage_IncludesPropertyNames()
    {
        var attribute = new RequiredIfAttribute("Status", "Active");

        var message = attribute.FormatErrorMessage("RequiredField");

        Assert.Multiple(() => {
            Assert.That(message, Does.Contain("RequiredField"));
            Assert.That(message, Does.Contain("Status"));
            Assert.That(message, Does.Contain("Active"));
        });
    }

    [Test]
    public void FormatErrorMessage_WithInverted_IncludesOtherThanText()
    {
        var attribute = new RequiredIfAttribute("Status", "Inactive", ComparisonType.NotEqualTo);

        var message = attribute.FormatErrorMessage("Field");

        Assert.That(message, Does.Contain(nameof(ComparisonType.NotEqualTo)));
    }
}