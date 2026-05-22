using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class DateRangeTests
{
    [Fact]
    public void From_must_be_le_To()
    {
        Should.Throw<ArgumentException>(() => new DateRange(
            new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 1)));
    }

    [Fact]
    public void Single_day_range_is_valid()
    {
        var d = new DateOnly(2026, 5, 22);
        var r = new DateRange(d, d);
        r.Days.ShouldBe([d]);
    }

    [Fact]
    public void Days_enumerates_inclusive()
    {
        var r = new DateRange(new DateOnly(2026, 5, 20), new DateOnly(2026, 5, 22));
        r.Days.ShouldBe([
            new DateOnly(2026, 5, 20),
            new DateOnly(2026, 5, 21),
            new DateOnly(2026, 5, 22),
        ]);
    }

    [Fact]
    public void Contains_checks_inclusive_bounds()
    {
        var r = new DateRange(new DateOnly(2026, 5, 20), new DateOnly(2026, 5, 22));
        r.Contains(new DateOnly(2026, 5, 20)).ShouldBeTrue();
        r.Contains(new DateOnly(2026, 5, 22)).ShouldBeTrue();
        r.Contains(new DateOnly(2026, 5, 19)).ShouldBeFalse();
    }
}
