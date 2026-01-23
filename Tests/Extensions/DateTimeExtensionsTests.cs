using CoreOne.Extensions;
using NUnit.Framework;

namespace Tests.Extensions;

public class DateTimeExtensionsTests
{
    [Test]
    public void CalculateAge_ReturnsCorrectAge()
    {
        var birthDate = new DateTime(1990, 6, 15);
        var targetDate = new DateTime(2020, 6, 15);
        var age = birthDate.CalculateAge(targetDate);
        Assert.That(age, Is.EqualTo(30));
    }

    [Test]
    public void CalculateAge_BeforeBirthday_ReturnsCorrectAge()
    {
        var birthDate = new DateTime(1990, 6, 15);
        var targetDate = new DateTime(2020, 6, 14);
        var age = birthDate.CalculateAge(targetDate);
        Assert.That(age, Is.EqualTo(29));
    }

    [Test]
    public void CalculateAge_AfterBirthday_ReturnsCorrectAge()
    {
        var birthDate = new DateTime(1990, 6, 15);
        var targetDate = new DateTime(2020, 6, 16);
        var age = birthDate.CalculateAge(targetDate);
        Assert.That(age, Is.EqualTo(30));
    }

    [Test]
    public void StartOfWeek_Sunday_ReturnsCorrectDate()
    {
        var date = new DateTime(2026, 1, 22); // Thursday
        var startOfWeek = date.StartOfWeek(DayOfWeek.Sunday);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(startOfWeek.DayOfWeek, Is.EqualTo(DayOfWeek.Sunday));
            Assert.That(startOfWeek, Is.EqualTo(new DateTime(2026, 1, 18)));
        }
    }

    [Test]
    public void StartOfWeek_Monday_ReturnsCorrectDate()
    {
        var date = new DateTime(2026, 1, 22); // Thursday
        var startOfWeek = date.StartOfWeek(DayOfWeek.Monday);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(startOfWeek.DayOfWeek, Is.EqualTo(DayOfWeek.Monday));
            Assert.That(startOfWeek, Is.EqualTo(new DateTime(2026, 1, 19)));
        }
    }

    [Test]
    public void StartOfWeek_SameAsStartDay_ReturnsSameDate()
    {
        var date = new DateTime(2026, 1, 18); // Sunday
        var startOfWeek = date.StartOfWeek(DayOfWeek.Sunday);
        Assert.That(startOfWeek, Is.EqualTo(date.Date));
    }

    [Test]
    public void TimeAgo_RecentSeconds_ReturnsSecondsAgo()
    {
        var stamp = DateTime.Now.AddSeconds(-30);
        var result = stamp.TimeAgo();
        Assert.That(result, Does.Contain("seconds ago"));
    }

    [Test]
    public void TimeAgo_OneMinute_ReturnsMinuteAgo()
    {
        var stamp = DateTime.Now.AddMinutes(-1);
        var result = stamp.TimeAgo();
        Assert.That(result, Is.EqualTo("a minute ago"));
    }

    [Test]
    public void TimeAgo_MultipleMinutes_ReturnsMinutesAgo()
    {
        var stamp = DateTime.Now.AddMinutes(-30);
        var result = stamp.TimeAgo();
        Assert.That(result, Does.Contain("minutes ago"));
    }

    [Test]
    public void TimeAgo_OneHour_ReturnsHourAgo()
    {
        var stamp = DateTime.Now.AddHours(-1);
        var result = stamp.TimeAgo();
        Assert.That(result, Is.EqualTo("an hour ago"));
    }

    [Test]
    public void TimeAgo_MultipleHours_ReturnsHoursAgo()
    {
        var stamp = DateTime.Now.AddHours(-5);
        var result = stamp.TimeAgo();
        Assert.That(result, Does.Contain("hours ago"));
    }

    [Test]
    public void TimeAgo_OneDay_ReturnsYesterday()
    {
        var stamp = DateTime.Now.AddHours(-25);
        var result = stamp.TimeAgo();
        Assert.That(result, Is.EqualTo("yesterday"));
    }

    [Test]
    public void TimeAgo_MultipleDays_ReturnsDaysAgo()
    {
        var stamp = DateTime.Now.AddDays(-10);
        var result = stamp.TimeAgo();
        Assert.That(result, Does.Contain("days ago"));
    }

    [Test]
    public void TimeAgo_OneMonth_ReturnsMonthAgo()
    {
        var stamp = DateTime.Now.AddDays(-35);
        var result = stamp.TimeAgo();
        Assert.That(result, Is.EqualTo("one month ago"));
    }

    [Test]
    public void TimeAgo_MultipleMonths_ReturnsMonthsAgo()
    {
        var stamp = DateTime.Now.AddDays(-90);
        var result = stamp.TimeAgo();
        Assert.That(result, Does.Contain("months ago"));
    }

    [Test]
    public void TimeAgo_OneYear_ReturnsYearAgo()
    {
        var stamp = DateTime.Now.AddDays(-400);
        var result = stamp.TimeAgo();
        Assert.That(result, Is.EqualTo("one year ago"));
    }

    [Test]
    public void TimeAgo_MultipleYears_ReturnsYearsAgo()
    {
        var stamp = DateTime.Now.AddDays(-800);
        var result = stamp.TimeAgo();
        Assert.That(result, Does.Contain("years ago"));
    }

    [Test]
    public void TimeAgo_NullableWithValue_ReturnsTimeAgo()
    {
        DateTime? stamp = DateTime.Now.AddMinutes(-5);
        var result = stamp.TimeAgo();
        Assert.That(result, Does.Contain("minutes ago"));
    }

    [Test]
    public void TimeAgo_NullableWithoutValue_ReturnsEmptyString()
    {
        DateTime? stamp = null;
        var result = stamp.TimeAgo();
        Assert.That(result, Is.EqualTo(""));
    }
}
