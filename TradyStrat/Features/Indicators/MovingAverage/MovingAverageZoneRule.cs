using TradyStrat.Common.Domain;
using TradyStrat.Features.Indicators.Zones;

namespace TradyStrat.Features.Indicators.MovingAverage;

public sealed class MovingAverageZoneRule : IZoneRule
{
    public string Name => "SMA50/200";

    public ZoneVote? Apply(decimal price, IndicatorBundle r)
    {
        if (r.Sma50 is not { } s50 || r.Sma200 is not { } s200) return null;

        if (price < s200) return new(Zone.Accumulate, $"Below 200-SMA ({s200:F2})");
        if (price > s50)  return new(Zone.Distribute, $"Above 50-SMA ({s50:F2})");
        return new(Zone.Hold, "Between 50/200-SMA");
    }
}
