using CoreOne.Extensions;
using CoreOne.Operations;

namespace Tests.Extensions;

public class ComparableExtensionsTests
{
    public class CustomEqualClass(int value)
    {
        public int Value { get; set; } = value;

        public override bool Equals(object? obj) => obj is CustomEqualClass ce && Value == ce.Value;
    }

    // Helper class for testing non-IComparable types
    private class NonComparableClass
    {
        public int Value { get; set; }
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
    public void Bounds_ValueEqualsMax_ReturnsValue()
    {
        var value = 10;
        var result = value.Bounds(1, 10);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Bounds_ValueEqualsMin_ReturnsValue()
    {
        var value = 1;
        var result = value.Bounds(1, 10);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Bounds_ValueWithinBounds_ReturnsValue()
    {
        var value = 5;
        var result = value.Bounds(1, 10);
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
    public void Ceiling_ValueBelowMax_ReturnsValue()
    {
        var value = 5;
        var result = value.Ceiling(10);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Ceiling_ValueEqualsMax_ReturnsMax()
    {
        var value = 10;
        var result = value.Ceiling(10);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void CompareToObject_BothNull_NonEqualityComparisons_ReturnFalse()
    {
        object? sourceValue = null;
        object? targetValue = null;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.LessThan), Is.False);
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThan), Is.False);
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.LessThanOrEqualTo), Is.False);
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThanOrEqualTo), Is.False);
        }
    }

    [Test]
    public void CompareToObject_EmptyStrings_WorksCorrectly()
    {
        object sourceValue = "";
        object targetValue = "";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.EqualTo), Is.True);
            Assert.That(sourceValue.CompareToObject("a", ComparisonType.LessThan), Is.True);
        }
    }

    [Test]
    public void CompareToObject_Equal_CustomEquals_ReturnsFalse()
    {
        object sourceValue = new CustomEqualClass(10);
        object? targetValue = null;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.EqualTo);
        Assert.That(result, Is.False);

        targetValue = new CustomEqualClass(9);
        result = sourceValue.CompareToObject(targetValue, ComparisonType.EqualTo);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareToObject_Equal_CustomEquals_ReturnsTrue()
    {
        object sourceValue = new CustomEqualClass(10);
        object? targetValue = new CustomEqualClass(10);
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.EqualTo);
        Assert.That(result, Is.True);
    }

    [Test]
    public void CompareToObject_Equal_CustomEquals_Throw()
    {
        object sourceValue = new CustomEqualClass(10);
        object? targetValue = new CustomEqualClass(10);
        Assert.Throws<ArgumentException>(() => sourceValue.CompareToObject(targetValue, ComparisonType.LessThan));
    }

    [Test]
    public void CompareToObject_EqualTo_BothNull_ReturnsTrue()
    {
        object? sourceValue = null;
        object? targetValue = null;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.EqualTo);
        Assert.That(result, Is.True);
    }

    [Test]
    public void CompareToObject_EqualTo_SourceNull_ReturnsFalse()
    {
        object? sourceValue = null;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.EqualTo);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareToObject_EqualTo_TargetNull_ReturnsFalse()
    {
        object sourceValue = 10;
        object? targetValue = null;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.EqualTo);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareToObject_EqualTo_ValuesAreEqual_ReturnsTrue()
    {
        object sourceValue = 10;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.EqualTo);
        Assert.That(result, Is.True);
    }

    [Test]
    public void CompareToObject_EqualTo_ValuesAreNotEqual_ReturnsFalse()
    {
        object sourceValue = 5;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.EqualTo);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareToObject_GreaterThan_SourceIsNull_ReturnsFalse()
    {
        object? sourceValue = null;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThan);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareToObject_GreaterThan_TargetIsNull_ReturnsFalse()
    {
        object sourceValue = 10;
        object? targetValue = null;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThan);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareToObject_GreaterThan_ValueIsEqual_ReturnsFalse()
    {
        object sourceValue = 10;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThan);
        Assert.That(result, Is.False);
    }

    // CompareToObject tests - GreaterThan
    [Test]
    public void CompareToObject_GreaterThan_ValueIsGreater_ReturnsTrue()
    {
        object sourceValue = 15;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThan);
        Assert.That(result, Is.True);
    }

    [Test]
    public void CompareToObject_GreaterThan_ValueIsLess_ReturnsFalse()
    {
        object sourceValue = 5;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThan);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareToObject_GreaterThanOrEqualTo_SourceIsNull_ReturnsFalse()
    {
        object? sourceValue = null;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThanOrEqualTo);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareToObject_GreaterThanOrEqualTo_ValueIsEqual_ReturnsTrue()
    {
        object sourceValue = 10;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThanOrEqualTo);
        Assert.That(result, Is.True);
    }

    // CompareToObject tests - GreaterThanOrEqualTo
    [Test]
    public void CompareToObject_GreaterThanOrEqualTo_ValueIsGreater_ReturnsTrue()
    {
        object sourceValue = 15;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThanOrEqualTo);
        Assert.That(result, Is.True);
    }

    [Test]
    public void CompareToObject_GreaterThanOrEqualTo_ValueIsLess_ReturnsFalse()
    {
        object sourceValue = 5;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThanOrEqualTo);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareToObject_LessThan_SourceIsNull_ReturnsFalse()
    {
        object? sourceValue = null;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.LessThan);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareToObject_LessThan_TargetIsNull_ReturnsFalse()
    {
        object sourceValue = 10;
        object? targetValue = null;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.LessThan);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareToObject_LessThan_ValueIsEqual_ReturnsFalse()
    {
        object sourceValue = 10;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.LessThan);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareToObject_LessThan_ValueIsGreater_ReturnsFalse()
    {
        object sourceValue = 15;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.LessThan);
        Assert.That(result, Is.False);
    }

    // CompareToObject tests - LessThan
    [Test]
    public void CompareToObject_LessThan_ValueIsLess_ReturnsTrue()
    {
        object sourceValue = 5;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.LessThan);
        Assert.That(result, Is.True);
    }

    [Test]
    public void CompareToObject_LessThanOrEqualTo_SourceIsNull_ReturnsFalse()
    {
        object? sourceValue = null;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.LessThanOrEqualTo);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareToObject_LessThanOrEqualTo_ValueIsEqual_ReturnsTrue()
    {
        object sourceValue = 10;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.LessThanOrEqualTo);
        Assert.That(result, Is.True);
    }

    [Test]
    public void CompareToObject_LessThanOrEqualTo_ValueIsGreater_ReturnsFalse()
    {
        object sourceValue = 15;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.LessThanOrEqualTo);
        Assert.That(result, Is.False);
    }

    // CompareToObject tests - LessThanOrEqualTo
    [Test]
    public void CompareToObject_LessThanOrEqualTo_ValueIsLess_ReturnsTrue()
    {
        object sourceValue = 5;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.LessThanOrEqualTo);
        Assert.That(result, Is.True);
    }

    // CompareToObject tests - Edge cases
    [Test]
    public void CompareToObject_NegativeNumbers_WorksCorrectly()
    {
        object sourceValue = -5;
        object targetValue = -10;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThan), Is.True);
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.LessThan), Is.False);
        }
    }

    [Test]
    public void CompareToObject_NonIComparable_EqualTo_DifferentReference_ReturnsFalse()
    {
        object sourceValue = new NonComparableClass { Value = 10 };
        object targetValue = new NonComparableClass { Value = 10 };
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.EqualTo);
        Assert.That(result, Is.False);
    }

    // CompareToObject tests - Non-IComparable types
    [Test]
    public void CompareToObject_NonIComparable_EqualTo_SameReference_ReturnsTrue()
    {
        var obj = new NonComparableClass { Value = 10 };
        object sourceValue = obj;
        object targetValue = obj;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.EqualTo);
        Assert.That(result, Is.True);
    }

    [Test]
    public void CompareToObject_NonIComparable_GreaterThan_ThrowsArgumentException()
    {
        object sourceValue = new NonComparableClass { Value = 10 };
        object targetValue = new NonComparableClass { Value = 5 };
        var ex = Assert.Throws<ArgumentException>(() =>
            sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThan));
        Assert.That(ex?.Message, Does.Contain("does not implement IComparable"));
    }

    [Test]
    public void CompareToObject_NonIComparable_GreaterThanOrEqualTo_ThrowsArgumentException()
    {
        object sourceValue = new NonComparableClass { Value = 10 };
        object targetValue = new NonComparableClass { Value = 5 };
        var ex = Assert.Throws<ArgumentException>(() =>
            sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThanOrEqualTo));
        Assert.That(ex?.Message, Does.Contain("does not implement IComparable"));
    }

    [Test]
    public void CompareToObject_NonIComparable_LessThan_ThrowsArgumentException()
    {
        object sourceValue = new NonComparableClass { Value = 10 };
        object targetValue = new NonComparableClass { Value = 5 };
        var ex = Assert.Throws<ArgumentException>(() =>
            sourceValue.CompareToObject(targetValue, ComparisonType.LessThan));
        Assert.That(ex?.Message, Does.Contain("does not implement IComparable"));
    }

    [Test]
    public void CompareToObject_NonIComparable_LessThanOrEqualTo_ThrowsArgumentException()
    {
        object sourceValue = new NonComparableClass { Value = 10 };
        object targetValue = new NonComparableClass { Value = 5 };
        var ex = Assert.Throws<ArgumentException>(() =>
            sourceValue.CompareToObject(targetValue, ComparisonType.LessThanOrEqualTo));
        Assert.That(ex?.Message, Does.Contain("does not implement IComparable"));
    }

    [Test]
    public void CompareToObject_NonIComparable_NotEqualTo_DifferentReference_ReturnsFalse()
    {
        object sourceValue = new NonComparableClass { Value = 10 };
        object targetValue = new NonComparableClass { Value = 10 };
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.NotEqualTo);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareToObject_NonIComparable_NotEqualTo_SameReference_ReturnsTrue()
    {
        var obj = new NonComparableClass { Value = 10 };
        object sourceValue = obj;
        object targetValue = obj;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.NotEqualTo);
        Assert.That(result, Is.True);
    }

    [Test]
    public void CompareToObject_NotEqualTo_BothNull_ReturnsFalse()
    {
        object? sourceValue = null;
        object? targetValue = null;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.NotEqualTo);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareToObject_NotEqualTo_SourceNull_ReturnsTrue()
    {
        object? sourceValue = null;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.NotEqualTo);
        Assert.That(result, Is.True);
    }

    [Test]
    public void CompareToObject_NotEqualTo_TargetNull_ReturnsTrue()
    {
        object sourceValue = 10;
        object? targetValue = null;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.NotEqualTo);
        Assert.That(result, Is.True);
    }

    [Test]
    public void CompareToObject_NotEqualTo_ValuesAreEqual_ReturnsFalse()
    {
        object sourceValue = 10;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.NotEqualTo);
        Assert.That(result, Is.False);
    }

    // CompareToObject tests - NotEqualTo
    [Test]
    public void CompareToObject_NotEqualTo_ValuesAreNotEqual_ReturnsTrue()
    {
        object sourceValue = 5;
        object targetValue = 10;
        var result = sourceValue.CompareToObject(targetValue, ComparisonType.NotEqualTo);
        Assert.That(result, Is.True);
    }

    // CompareToObject tests - Unsupported ComparisonType
    [Test]
    public void CompareToObject_UnsupportedComparisonType_ThrowsInvalidOperationException()
    {
        object sourceValue = 10;
        object targetValue = 5;
        var invalidComparisonType = (ComparisonType)999;
        var ex = Assert.Throws<InvalidOperationException>(() =>
            sourceValue.CompareToObject(targetValue, invalidComparisonType));
        Assert.That(ex?.Message, Does.Contain("Unsupported comparison type"));
    }

    [Test]
    public void CompareToObject_WorksWithDateTime()
    {
        object sourceValue = new DateTime(2024, 1, 1);
        object targetValue = new DateTime(2024, 12, 31);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.LessThan), Is.True);
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThan), Is.False);
            Assert.That(sourceValue.CompareToObject(new DateTime(2024, 1, 1), ComparisonType.EqualTo), Is.True);
        }
    }

    [Test]
    public void CompareToObject_WorksWithDoubles()
    {
        object sourceValue = 3.14;
        object targetValue = 2.71;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThan), Is.True);
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.LessThan), Is.False);
            Assert.That(sourceValue.CompareToObject(3.14, ComparisonType.EqualTo), Is.True);
        }
    }

    // CompareToObject tests - Different types
    [Test]
    public void CompareToObject_WorksWithStrings()
    {
        object sourceValue = "apple";
        object targetValue = "banana";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.LessThan), Is.True);
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThan), Is.False);
            Assert.That(sourceValue.CompareToObject("apple", ComparisonType.EqualTo), Is.True);
        }
    }

    [Test]
    public void CompareToObject_ZeroValues_WorksCorrectly()
    {
        object sourceValue = 0;
        object targetValue = 0;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.EqualTo), Is.True);
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.NotEqualTo), Is.False);
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.LessThanOrEqualTo), Is.True);
            Assert.That(sourceValue.CompareToObject(targetValue, ComparisonType.GreaterThanOrEqualTo), Is.True);
        }
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
    public void IsBetween_ValueEqualsMax_Exclusive_ReturnsFalse()
    {
        var value = 10;
        Assert.That(value.IsBetween(1, 10, inclusive: false), Is.False);
    }

    [Test]
    public void IsBetween_ValueEqualsMax_Inclusive_ReturnsTrue()
    {
        var value = 10;
        Assert.That(value.IsBetween(1, 10, inclusive: true), Is.True);
    }

    [Test]
    public void IsBetween_ValueEqualsMin_Exclusive_ReturnsFalse()
    {
        var value = 1;
        Assert.That(value.IsBetween(1, 10, inclusive: false), Is.False);
    }

    [Test]
    public void IsBetween_ValueEqualsMin_Inclusive_ReturnsTrue()
    {
        var value = 1;
        Assert.That(value.IsBetween(1, 10, inclusive: true), Is.True);
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