namespace CoreOne.Services;

public class FixedClock(DateTime utcTime) : IClock
{
    public DateTime UtcNow => utcTime;
}