namespace TradyStrat.Shared.Time;

public interface IClock
{
    DateTime UtcNow();
    DateOnly TodayLocal();
    DateOnly TodayInExchangeTzFor(string ticker);
}
