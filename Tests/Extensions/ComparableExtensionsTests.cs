using CoreOne.Extensions;
using NUnit.Framework;

namespace Tests.Extensions;

public class ComparableExtensionsTests
{
    [Test]
    public void Bounds_ValueWithinBounds_ReturnsValue()
    {
        var value = 5;
        var result = value.Bounds(1, 10);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Bounds_ValueAboveMax_ReturnsMax()
    {
        var value = 15;
        var result = value.Bounds(1, 10);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Bounds_ValueBelowMin_ReturnsMin()
    {
        var value = -5;
        var result = value.Bounds(1, 10);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Bounds_ValueEqualsMin_ReturnsValue()
    {
        var value = 1;
        var result = value.Bounds(1, 10);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Bounds_ValueEqualsMax_ReturnsValue()
    {
        var value = 10;
        var result = value.Bounds(1, 10);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Ceiling_ValueBelowMax_ReturnsValue()
    {
        var value = 5;
        var result = value.Ceiling(10);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Ceiling_ValueAboveMax_ReturnsMax()
    {
        var value = 15;
        var result = value.Ceiling(10);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Ceiling_ValueEqualsMax_ReturnsMax()
    {
        var value = 10;
        var result = value.Ceiling(10);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Floor_ValueAboveMin_ReturnsValue()
    {
        var value = 5;
        var result = value.Floor(1);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Floor_ValueBelowMin_ReturnsMin()
    {
        var value = -5;
        var result = value.Floor(1);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Floor_ValueEqualsMin_ReturnsMin()
    {
        var value = 1;
        var result = value.Floor(1);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void IsBetween_ValueInRange_Inclusive_ReturnsTrue()
    {
        var value = 5;
        Assert.That(value.IsBetween(1, 10), Is.True);
    }

    [Test]
    public void IsBetween_ValueOutOfRange_ReturnsFalse()
    {
        var value = 15;
        Assert.That(value.IsBetween(1, 10), Is.False);
    }

    [Test]
    public void IsBetween_ValueEqualsMin_Inclusive_ReturnsTrue()
    {
        var value = 1;
        Assert.That(value.IsBetween(1, 10, inclusive: true), Is.True);
    }

    [Test]
    public void IsBetween_ValueEqualsMin_Exclusive_ReturnsFalse()
    {
        var value = 1;
        Assert.That(value.IsBetween(1, 10, inclusive: false), Is.False);
    }

    [Test]
    public void IsBetween_ValueEqualsMax_Inclusive_ReturnsTrue()
    {
        var value = 10;
        Assert.That(value.IsBetween(1, 10, inclusive: true), Is.True);
    }

    [Test]
    public void IsBetween_ValueEqualsMax_Exclusive_ReturnsFalse()
    {
        var value = 10;
        Assert.That(value.IsBetween(1, 10, inclusive: false), Is.False);
    }

    [Test]
    public void IsBetween_WorksWithDoubles()
    {
        var value = 5.5;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(value.IsBetween(1.0, 10.0), Is.True);
            Assert.That(value.IsBetween(6.0, 10.0), Is.False);
        }
    }

    [Test]
    public void IsBetween_WorksWithStrings()
    {
        var value = "m";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(value.IsBetween("a", "z"), Is.True);
            Assert.That(value.IsBetween("n", "z"), Is.False);
        }
    }
}
