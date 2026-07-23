using TradyStrat.Domain;
using TradyStrat.Domain.Indicators.Services;

namespace TradyStrat.Application.Indicators.MovingAverage;

public sealed class MovingAverageZoneRule : IZoneRule
{
    public string Name => "SMA50/200";

    public ZoneVote? Apply(decimal price, IndicatorBundle r)
    {
        // Sma50/200 use 0m as the not-computed sentinel (zero is never a valid
        // price-derived SMA in this app's universe).
        if (r.Sma50 == 0m || r.Sma200 == 0m) return null;

        if (price < r.Sma200) return new(Zone.Accumulate, $"Below 200-SMA ({r.Sma200:F2})");
        if (price > r.Sma50)  return new(Zone.Distribute, $"Above 50-SMA ({r.Sma50:F2})");
        return new(Zone.Hold, "Between 50/200-SMA");
    }
}
