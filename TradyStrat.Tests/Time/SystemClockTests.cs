using Shouldly;
using TradyStrat.Common.Time;
using Xunit;

namespace TradyStrat.Tests.Time;

public class SystemClockTests
{
    [Fact]
    public void TodayInExchangeTzFor_CON3_returns_today_in_Berlin()
    {
        var c = new SystemClock();
        var today = c.TodayInExchangeTzFor("CON3.DE");
        var expected = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")));

        today.ShouldBe(expected);
    }

    [Theory]
    [InlineData("COIN",    "America/New_York")]
    [InlineData("BTC-USD", "Etc/UTC")]
    [InlineData("EURUSD",  "Etc/UTC")]
    public void TodayInExchangeTzFor_uses_correct_zone(string ticker, string tzId)
    {
        var c = new SystemClock();
        var actual = c.TodayInExchangeTzFor(ticker);
        var expected = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById(tzId)));

        actual.ShouldBe(expected);
    }

    [Fact]
    public void UtcNow_returns_kind_utc()
    {
        new SystemClock().UtcNow().Kind.ShouldBe(DateTimeKind.Utc);
    }
}
