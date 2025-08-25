namespace CoreOne.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Get age from date of birth
    /// </summary>
    /// <param name="dateOfBirth"></param>
    /// <param name="targetDate"></param>
    /// <returns></returns>
    public static int CalculateAge(this DateTime dateOfBirth, DateTime targetDate)
    { // Src: https://stackoverflow.com/a/16142434/216578
        return (((targetDate.Year - dateOfBirth.Year) * 372) + ((targetDate.Month - dateOfBirth.Month) * 31) + (targetDate.Day - dateOfBirth.Day)) / 372;
    }

    /// <summary>
    /// Get age from date of birth
    /// </summary>
    /// <param name="dateOfBirth"></param>
    /// <returns></returns>
    public static int CalculateAge(this DateTime dateOfBirth) => CalculateAge(dateOfBirth, DateTime.Now);

#if NET9_0_OR_GREATER
    /// <summary>
    /// Get age from date of birth
    /// </summary>
    /// <param name="dateOfBirth"></param>
    /// <param name="targetDate"></param>
    /// <returns></returns>
    public static int CalculateAge(DateOnly dateOfBirth, DateOnly targetDate)
    { // Src: https://stackoverflow.com/a/16142434/216578
        return (((targetDate.Year - dateOfBirth.Year) * 372) + ((targetDate.Month - dateOfBirth.Month) * 31) + (targetDate.Day - dateOfBirth.Day)) / 372;
    }
 
    /// <summary>
    /// Get age from date of birth
    /// </summary>
    /// <param name="dateOfBirth"></param>
    /// <returns></returns>
    public static int CalculateAge(this DateOnly dateOfBirth) => CalculateAge(dateOfBirth, DateOnly.FromDateTime(DateTime.Now));
#endif
    /// <summary>
    /// Gets the date of the first day of the week for the given date.
    /// </summary>
    /// <param name="date"></param>
    /// <param name="startOfWeek"></param>
    /// <returns></returns>
    public static DateTime StartOfWeek(this DateTime date, DayOfWeek startOfWeek = DayOfWeek.Sunday)
    {
        int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    public static string TimeAgo(this DateTime? stamp) => stamp.HasValue ? stamp.Value.TimeAgo() : "";

    public static string TimeAgo(this DateTime stamp)
    {
        const int SECOND = 1;
        const int MINUTE = 60 * SECOND;
        const int HOUR = 60 * MINUTE;
        const int DAY = 24 * HOUR;
        const int MONTH = 30 * DAY;
        var ts = new TimeSpan(DateTime.Now.Ticks - stamp.Ticks);
        double delta = Math.Abs(ts.TotalSeconds);

        if (delta < 1 * MINUTE)
            return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";

        if (delta < 2 * MINUTE)
            return "a minute ago";

        if (delta < 45 * MINUTE)
            return ts.Minutes + " minutes ago";

        if (delta < 90 * MINUTE)
            return "an hour ago";

        if (delta < 24 * HOUR)
            return ts.Hours + " hours ago";

        if (delta < 48 * HOUR)
            return "yesterday";

        if (delta < 30 * DAY)
            return ts.Days + " days ago";

        if (delta < 12 * MONTH)
        {
            int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
            return months <= 1 ? "one month ago" : months + " months ago";
        }
        int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
        return years <= 1 ? "one year ago" : years + " years ago";
    }
}