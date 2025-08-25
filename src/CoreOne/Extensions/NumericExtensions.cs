namespace CoreOne.Extensions;

public static class NumericExtensions
{
    /// <summary>
    /// Performs linear interpolation between two <see cref="float"/> values.
    /// </summary>
    /// <param name="start">The starting value.</param>
    /// <param name="end">The ending value.</param>
    /// <param name="percent">A value between 0 and 1 representing the interpolation factor.</param>
    /// <returns>The interpolated float value.</returns>
    public static float Lerp(this float start, float end, float percent) => start + ((end - start) * percent);

    /// <summary>
    /// Performs linear interpolation between two <see cref="double"/> values.
    /// </summary>
    /// <param name="start">The starting value.</param>
    /// <param name="end">The ending value.</param>
    /// <param name="percent">A value between 0 and 1 representing the interpolation factor.</param>
    /// <returns>The interpolated double value.</returns>
    public static double Lerp(this double start, double end, float percent) => start + ((end - start) * percent);

    /// <summary>
    /// Performs linear interpolation between two <see cref="int"/> values.
    /// </summary>
    /// <param name="start">The starting value.</param>
    /// <param name="end">The ending value.</param>
    /// <param name="percent">A value between 0 and 1 representing the interpolation factor.</param>
    /// <returns>The interpolated integer value, rounded from the result.</returns>
    public static int Lerp(this int start, int end, float percent) => start + Convert.ToInt32((end - start) * percent);

#if NET9_0_OR_GREATER
    /// <summary>
    /// Scales a numeric value from one range to another.
    /// </summary>
    /// <typeparam name="T">A numeric type that implements <see cref=" System.Numerics.INumber{T}"/>.</typeparam>
    /// <param name="x">The value to scale.</param>
    /// <param name="in_min">The minimum of the input range.</param>
    /// <param name="in_max">The maximum of the input range.</param>
    /// <param name="out_min">The minimum of the output range.</param>
    /// <param name="out_max">The maximum of the output range.</param>
    /// <returns>The value of <paramref name="x"/> scaled to the output range.</returns>
    public static T Scale<T>(this T x, T in_min, T in_max, T out_min, T out_max) where T : System.Numerics.INumber<T>
        => ((x - in_min) * (out_max - out_min) / (in_max - in_min)) + out_min;
#endif
}