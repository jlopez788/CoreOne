namespace CoreOne;

public partial class Utility
{
#if NET9_0_OR_GREATER
    public static int Next(int minVal, int maxVal)
    {
        int rand = minVal < maxVal ? Random.Shared.Next(minVal, maxVal) : Random.Shared.Next(maxVal, minVal);
        return rand;
    }
#endif
}