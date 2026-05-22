using TradyStrat.Domain;
using TradyStrat.Application.Indicators.Zones;

namespace TradyStrat.Application.Indicators.Rsi;

public sealed class RsiZoneRule : IZoneRule
{
    public string Name => "RSI";

    public ZoneVote? Apply(decimal price, IndicatorBundle r)
    {
        if (r.Rsi.IsEmpty) return null;
        var v = r.Rsi.Value;
        if (v < 30m) return new(Zone.Accumulate, $"RSI(14) {v:F0}, oversold");
        if (v > 70m) return new(Zone.Distribute, $"RSI(14) {v:F0}, overbought");
        return new(Zone.Hold, $"RSI(14) {v:F0}, neutral");
    }
}
