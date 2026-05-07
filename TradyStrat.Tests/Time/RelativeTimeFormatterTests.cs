using Shouldly;
using TradyStrat.Common.Time;
using Xunit;

namespace TradyStrat.Tests.Time;

public class RelativeTimeFormatterTests
{
    private static readonly DateTime Now = new(2026, 5, 7, 18, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0,    "just now")]
    [InlineData(45,   "just now")]   // < 60 s
    public void Just_now_bucket(int secondsAgo, string expected)
        => RelativeTimeFormatter.Format(Now.AddSeconds(-secondsAgo), Now).ShouldBe(expected);

    [Theory]
    [InlineData(60,    "1 min ago")]
    [InlineData(60*12, "12 min ago")]
    [InlineData(60*59, "59 min ago")]
    public void Minutes_bucket(int secondsAgo, string expected)
        => RelativeTimeFormatter.Format(Now.AddSeconds(-secondsAgo), Now).ShouldBe(expected);

    [Theory]
    [InlineData(60*60,    "1h ago")]
    [InlineData(60*60*14, "14h ago")]
    [InlineData(60*60*23, "23h ago")]
    public void Hours_bucket(int secondsAgo, string expected)
        => RelativeTimeFormatter.Format(Now.AddSeconds(-secondsAgo), Now).ShouldBe(expected);

    [Fact]
    public void Yesterday_when_exactly_one_calendar_day_back()
    {
        var asOf = new DateTime(2026, 5, 6, 18, 0, 0, DateTimeKind.Utc);  // exactly 24h
        RelativeTimeFormatter.Format(asOf, Now).ShouldBe("yesterday");
    }

    [Fact]
    public void Days_bucket_two_to_six()
    {
        var asOf = Now.AddDays(-3);
        RelativeTimeFormatter.Format(asOf, Now).ShouldBe("3 days ago");
    }

    [Fact]
    public void Absolute_when_seven_or_more_days()
    {
        var asOf = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);  // ~25 days back
        RelativeTimeFormatter.Format(asOf, Now).ShouldBe("12 apr");
    }
}
