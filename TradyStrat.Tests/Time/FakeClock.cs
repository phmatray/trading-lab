using TradyStrat.Common.Time;

namespace TradyStrat.Tests.Time;

public sealed class FakeClock(DateTime utcNow) : IClock
{
    public DateTime Now { get; set; } = utcNow;

    public DateTime UtcNow() => Now;
    public DateOnly TodayLocal() => DateOnly.FromDateTime(Now);
    public DateOnly TodayInExchangeTzFor(string ticker) => DateOnly.FromDateTime(Now);
}
