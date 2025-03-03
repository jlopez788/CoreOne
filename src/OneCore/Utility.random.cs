namespace OneCore;

public partial class Utility
{
    public static int Next(int minVal, int maxVal)
    {
        int rand = minVal < maxVal ? Random.Shared.Next(minVal, maxVal) : Random.Shared.Next(maxVal, minVal);
        return rand;
    }
}