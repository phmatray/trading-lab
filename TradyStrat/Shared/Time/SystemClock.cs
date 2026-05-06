namespace TradyStrat.Shared.Time;

public sealed class SystemClock : IClock
{
    private static readonly Dictionary<string, string> TzByTicker = new()
    {
        ["CON3.DE"] = "Europe/Berlin",
        ["COIN"]    = "America/New_York",
        ["BTC-USD"] = "Etc/UTC",
        ["EURUSD"]  = "Etc/UTC",
    };

    public DateTime UtcNow() => DateTime.UtcNow;

    public DateOnly TodayLocal() => DateOnly.FromDateTime(DateTime.Now);

    public DateOnly TodayInExchangeTzFor(string ticker)
    {
        var tzId = TzByTicker.TryGetValue(ticker, out var z) ? z : "Etc/UTC";
        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz));
    }
}
