namespace OneCore.Extensions;

/// <summary>
/// Some extension methods for <see cref="Random"/> for creating a few more kinds of random stuff.
/// </summary>
public static class RandomExtensions
{
    /// <summary>
    ///   Equally likely to return true or false. Uses <see cref="Random.Next()"/>.
    /// </summary>
    /// <returns></returns>
    public static bool NextBoolean(this Random r) => r.Next(2) > 0;

    /// <summary>
    ///   Generates normally distributed numbers. Each operation makes two Gaussians for the price of one, and apparently they can be cached or something for better performance, but who cares.
    /// </summary>
    /// <param name="r"></param>
    /// <param name = "mu">Mean of the distribution</param>
    /// <param name = "sigma">Standard deviation</param>
    /// <returns></returns>
    public static double NextGaussian(this Random r, double mu = 0, double sigma = 1)
    {
        var u1 = r.NextDouble();
        var u2 = r.NextDouble();
        var rand_std_normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        var rand_normal = mu + (sigma * rand_std_normal);
        return rand_normal;
    }

    /// <summary>
    /// Returns n unique random numbers in the range [1, n], inclusive.
    /// This is equivalent to getting the first n numbers of some random permutation of the sequential numbers from 1 to max.
    /// Runs in O(k^2) time.
    /// </summary>
    /// <param name="rand"></param>
    /// <param name="n">Maximum number possible.</param>
    /// <param name="k">How many numbers to return.</param>
    /// <returns></returns>
    public static int[] Permutation(this Random rand, int n, int k)
    {
        var result = new List<int>();
        var sorted = new SortedSet<int>();
        for (var i = 0; i < k; i++)
        {
            var r = rand.Next(1, n + 1 - i);
            foreach (var q in sorted)
                if (r >= q)
                    r++;

            result.Add(r);
            sorted.Add(r);
        }

        return [.. result];
    }
}