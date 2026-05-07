namespace TradyStrat.Common.Time;

public interface IClock
{
    DateTime UtcNow();
    DateOnly TodayLocal();
    DateOnly TodayInExchangeTzFor(string ticker);
}
