using CoreOne.Attributes;

namespace Tests.Attributes;

[TestFixture]
public class DateValidationAttributeTests
{
    [Test]
    public void IsValid_WithValidDateTime_ReturnsTrue()
    {
        var attribute = new DateValidationAttribute();
        var validDate = new DateTime(2024, 1, 15);

        var result = attribute.IsValid(validDate);

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsValid_WithYear1900_ReturnsFalse()
    {
        var attribute = new DateValidationAttribute();
        var date1900 = new DateTime(1900, 1, 1);

        var result = attribute.IsValid(date1900);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValid_WithYear1901_ReturnsTrue()
    {
        var attribute = new DateValidationAttribute();
        var date1901 = new DateTime(1901, 1, 1);

        var result = attribute.IsValid(date1901);

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsValid_WithMinDateTime_ReturnsFalse()
    {
        var attribute = new DateValidationAttribute();

        var result = attribute.IsValid(DateTime.MinValue);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValid_WithMaxDateTime_ReturnsTrue()
    {
        var attribute = new DateValidationAttribute();

        var result = attribute.IsValid(DateTime.MaxValue);

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsValid_WithNull_WhenAllowNullFalse_ReturnsFalse()
    {
        var attribute = new DateValidationAttribute { AllowNull = false };

        var result = attribute.IsValid(null);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValid_WithNull_WhenAllowNullTrue_ReturnsTrue()
    {
        var attribute = new DateValidationAttribute { AllowNull = true };

        var result = attribute.IsValid(null);

        Assert.That(result, Is.True);
    }

#if NET9_0_OR_GREATER
    [Test]
    public void IsValid_WithValidDateOnly_ReturnsTrue()
    {
        var attribute = new DateValidationAttribute();
        var validDate = new DateOnly(2024, 6, 15);

        var result = attribute.IsValid(validDate);

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsValid_WithMinDateOnly_ReturnsFalse()
    {
        var attribute = new DateValidationAttribute();

        var result = attribute.IsValid(DateOnly.MinValue);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValid_WithDateOnly1901_ReturnsTrue()
    {
        var attribute = new DateValidationAttribute();
        var date1901 = new DateOnly(1901, 1, 1);

        var result = attribute.IsValid(date1901);

        Assert.That(result, Is.True);
    }
#endif

    [Test]
    public void IsValid_WithNonDateType_ReturnsFalse()
    {
        var attribute = new DateValidationAttribute();

        var result = attribute.IsValid("not a date");

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValid_WithInteger_ReturnsFalse()
    {
        var attribute = new DateValidationAttribute();

        var result = attribute.IsValid(12345);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsValid_DateTimeCheckOnlyDate_IgnoresTime()
    {
        var attribute = new DateValidationAttribute();
        var dateWithTime = new DateTime(2024, 1, 15, 14, 30, 45);

        var result = attribute.IsValid(dateWithTime);

        Assert.That(result, Is.True);
    }
}
