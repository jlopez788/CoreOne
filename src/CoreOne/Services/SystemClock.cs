namespace CoreOne.Services;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}