using CoreOne.Operations;

namespace CoreOne.Extensions;

public static class ComparableExtensions
{
    /// <summary>
    /// Get comparer between given upper and lower bounds
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public static T Bounds<T>(this T value, T min, T max) where T : IComparable<T> => value.CompareTo(max) > 0 ? max : value.CompareTo(min) < 0 ? min : value;

    /// <summary>
    /// Get value with a maximun upper bound limit
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public static T Ceiling<T>(this T value, T max) where T : IComparable<T> => (value.CompareTo(max) < 1) ? value : max;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceValue"></param>
    /// <param name="targetValue"></param>
    /// <param name="comparisonType"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static bool CompareToObject(this object? sourceValue, object? targetValue, ComparisonType comparisonType)
    {
        var sourceComparable = sourceValue as IComparable;
        var isSVNull = sourceComparable == null;
        var isTVNull = targetValue == null;

        if (sourceValue is not null || targetValue is not null)
        {
            var type = (sourceValue?.GetType() ?? targetValue?.GetType());
            if (!Types.IComparable.IsAssignableFrom(type))
            {
                var typeName = type?.Name;
                return comparisonType is ComparisonType.EqualTo or ComparisonType.NotEqualTo ?
                    ReferenceEqualityComparer.Default.Equals(sourceValue, targetValue) :
                    throw new ArgumentException(
                        $"Source value of type '{typeName}' does not implement IComparable interface. " +
                        $"Comparison operations require IComparable. Consider using ComparisonType.EqualTo/NotEqualTo " +
                        $"with types that implement IEquatable<T>, or implement IComparable on '{typeName}'.");
            }
        }

        return comparisonType switch {
            ComparisonType.LessThan => !isSVNull && !isTVNull && sourceComparable?.CompareTo(targetValue) < 0,
            ComparisonType.LessThanOrEqualTo => !isSVNull && !isTVNull && sourceComparable?.CompareTo(targetValue) <= 0,
            ComparisonType.NotEqualTo => (!isSVNull && isTVNull) || (isSVNull && !isTVNull) ||
                                         (!isSVNull && !isTVNull && sourceComparable?.CompareTo(targetValue) != 0),
            ComparisonType.EqualTo => (isSVNull && isTVNull) ||
                                      (!isSVNull && !isTVNull && sourceComparable?.CompareTo(targetValue) == 0),
            ComparisonType.GreaterThan => !isSVNull && !isTVNull && sourceComparable?.CompareTo(targetValue) > 0,
            ComparisonType.GreaterThanOrEqualTo => !isSVNull && !isTVNull && sourceComparable?.CompareTo(targetValue) >= 0,
            _ => throw new InvalidOperationException(
                $"Unsupported comparison type '{comparisonType}'. " +
                $"Supported types: EqualTo, NotEqualTo, GreaterThan, LessThan, GreaterThanOrEqualTo, LessThanOrEqualTo."),
        };
    }

    /// <summary>
    /// Get value with a minimum lower bound limit
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="min"></param>
    /// <returns></returns>
    public static T Floor<T>(this T value, T min) where T : IComparable<T> => (value.CompareTo(min) < 0) ? min : value;

    /// <summary>
    /// Checks if value is between given upper and lower bounds
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="inclusive"></param>
    /// <returns></returns>
    public static bool IsBetween<T>(this T value, T min, T max, bool inclusive = true) where T : IComparable<T> => inclusive
            ? (value.CompareTo(min) >= 0) && (value.CompareTo(max) <= 0)
            : (value.CompareTo(min) > 0) && (value.CompareTo(max) < 0);
}