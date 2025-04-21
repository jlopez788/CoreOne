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