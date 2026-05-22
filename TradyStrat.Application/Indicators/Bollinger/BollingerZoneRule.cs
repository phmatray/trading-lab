using TradyStrat.Domain;
using TradyStrat.Domain.Indicators.Services;

namespace TradyStrat.Application.Indicators.Bollinger;

public sealed class BollingerZoneRule : IZoneRule
{
    public string Name => "Bollinger";

    public ZoneVote? Apply(decimal price, IndicatorBundle r)
    {
        if (r.Bollinger.IsEmpty) return null;
        var bb = r.Bollinger;
        if (price < bb.Lower)
            return new(Zone.Accumulate, $"Price {price:F2} below lower Bollinger ({bb.Lower:F2})");
        if (price > bb.Upper)
            return new(Zone.Distribute, $"Price above upper Bollinger ({bb.Upper:F2})");
        return new(Zone.Hold, "Inside Bollinger band");
    }
}
