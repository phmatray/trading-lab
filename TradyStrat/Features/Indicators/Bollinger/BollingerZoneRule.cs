using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Indicators;

public sealed class BollingerZoneRule : IZoneRule
{
    public string Name => "Bollinger";

    public ZoneVote? Apply(decimal price, IndicatorBundle r) => r.Bollinger switch
    {
        null => null,
        var bb when price < bb.Lower => new(Zone.Accumulate,
            $"Price {price:F2} below lower Bollinger ({bb.Lower:F2})"),
        var bb when price > bb.Upper => new(Zone.Distribute,
            $"Price above upper Bollinger ({bb.Upper:F2})"),
        _ => new(Zone.Hold, "Inside Bollinger band"),
    };
}
