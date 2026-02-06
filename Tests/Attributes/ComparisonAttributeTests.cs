using CoreOne.Attributes;
using CoreOne.Models;
using System.ComponentModel.DataAnnotations;

namespace Tests.Attributes;

[TestFixture]
public class ComparisonAttributeTests
{
    private class TestModel
    {
        public string? ConfirmPassword { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxValue { get; set; }
        [Comparison(nameof(MaxValue), ComparisonType.LessThanOrEqualTo)]
        public int MinValue { get; set; }
        [Comparison(nameof(ConfirmPassword), ComparisonType.EqualTo)]
        public string? Password { get; set; }
        [Comparison(nameof(EndDate), ComparisonType.LessThan)]
        public DateTime StartDate { get; set; }
    }

    [Test]
    public void IsValid_EqualTo_WhenBothNull_ReturnsSuccess()
    {
        var model = new TestModel { Password = null, ConfirmPassword = null };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.Password) };
        var attribute = new ComparisonAttribute(nameof(TestModel.ConfirmPassword), ComparisonType.EqualTo);

        var result = attribute.GetValidationResult(model.Password, context);

        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_EqualTo_WhenValuesDiffer_ReturnsError()
    {
        var model = new TestModel { Password = "secret123", ConfirmPassword = "different" };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.Password) };
        var attribute = new ComparisonAttribute(nameof(TestModel.ConfirmPassword), ComparisonType.EqualTo);

        var result = attribute.GetValidationResult(model.Password, context);

        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_EqualTo_WhenValuesMatch_ReturnsSuccess()
    {
        var model = new TestModel { Password = "secret123", ConfirmPassword = "secret123" };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.Password) };
        var attribute = new ComparisonAttribute(nameof(TestModel.ConfirmPassword), ComparisonType.EqualTo);

        var result = attribute.GetValidationResult(model.Password, context);

        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_GreaterThan_WhenSourceIsGreater_ReturnsSuccess()
    {
        var model = new TestModel { MinValue = 20, MaxValue = 10 };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.MinValue) };
        var attribute = new ComparisonAttribute(nameof(TestModel.MaxValue), ComparisonType.GreaterThan);

        var result = attribute.GetValidationResult(model.MinValue, context);

        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_GreaterThanOrEqualTo_WhenEqual_ReturnsSuccess()
    {
        var model = new TestModel { MinValue = 10, MaxValue = 10 };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.MinValue) };
        var attribute = new ComparisonAttribute(nameof(TestModel.MaxValue), ComparisonType.GreaterThanOrEqualTo);

        var result = attribute.GetValidationResult(model.MinValue, context);

        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_LessThan_WhenSourceIsGreater_ReturnsError()
    {
        var model = new TestModel {
            StartDate = new DateTime(2024, 12, 31),
            EndDate = new DateTime(2024, 1, 1)
        };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.StartDate) };
        var attribute = new ComparisonAttribute(nameof(TestModel.EndDate), ComparisonType.LessThan);

        var result = attribute.GetValidationResult(model.StartDate, context);

        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_LessThan_WhenSourceIsLess_ReturnsSuccess()
    {
        var model = new TestModel {
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31)
        };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.StartDate) };
        var attribute = new ComparisonAttribute(nameof(TestModel.EndDate), ComparisonType.LessThan);

        var result = attribute.GetValidationResult(model.StartDate, context);

        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_LessThanOrEqualTo_WhenEqual_ReturnsSuccess()
    {
        var model = new TestModel { MinValue = 10, MaxValue = 10 };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.MinValue) };
        var attribute = new ComparisonAttribute(nameof(TestModel.MaxValue), ComparisonType.LessThanOrEqualTo);

        var result = attribute.GetValidationResult(model.MinValue, context);

        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_LessThanOrEqualTo_WhenLess_ReturnsSuccess()
    {
        var model = new TestModel { MinValue = 5, MaxValue = 10 };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.MinValue) };
        var attribute = new ComparisonAttribute(nameof(TestModel.MaxValue), ComparisonType.LessThanOrEqualTo);

        var result = attribute.GetValidationResult(model.MinValue, context);

        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_NotEqualTo_WhenValuesDiffer_ReturnsSuccess()
    {
        var model = new TestModel { MinValue = 5, MaxValue = 10 };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.MinValue) };
        var attribute = new ComparisonAttribute(nameof(TestModel.MaxValue), ComparisonType.NotEqualTo);

        var result = attribute.GetValidationResult(model.MinValue, context);

        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_NotEqualTo_WhenValuesSame_ReturnsError()
    {
        var model = new TestModel { MinValue = 10, MaxValue = 10 };
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.MinValue) };
        var attribute = new ComparisonAttribute(nameof(TestModel.MaxValue), ComparisonType.NotEqualTo);

        var result = attribute.GetValidationResult(model.MinValue, context);

        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WithDifferentTypes_ThrowsArgumentException()
    {
        var model = new { IntValue = 10, StringValue = "10" };
        var context = new ValidationContext(model) { MemberName = "IntValue" };
        var attribute = new ComparisonAttribute("StringValue", ComparisonType.EqualTo);

        Assert.Throws<ArgumentException>(() => attribute.GetValidationResult(model.IntValue, context));
    }

    [Test]
    public void IsValid_WithNonComparableType_ThrowsArgumentException()
    {
        var model = new { Value = new object(), Other = new object() };
        var context = new ValidationContext(model) { MemberName = "Value" };
        var attribute = new ComparisonAttribute("Other", ComparisonType.EqualTo);

        Assert.Throws<ArgumentException>(() => attribute.GetValidationResult(model.Value, context));
    }

    [Test]
    public void IsValid_WithNonExistentProperty_ThrowsArgumentException()
    {
        var model = new TestModel();
        var context = new ValidationContext(model) { MemberName = nameof(TestModel.MinValue) };
        var attribute = new ComparisonAttribute("NonExistentProperty", ComparisonType.EqualTo);

        Assert.Throws<ArgumentException>(() => attribute.GetValidationResult(model.MinValue, context));
    }
}