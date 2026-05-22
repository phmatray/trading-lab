using TradyStrat.Domain;
using TradyStrat.Domain.Indicators.Services;

namespace TradyStrat.Application.Indicators.Ichimoku;

public sealed class IchimokuZoneRule : IZoneRule
{
    public string Name => "Ichimoku";

    public ZoneVote? Apply(decimal price, IndicatorBundle r)
    {
        if (r.Ichimoku.IsEmpty) return null;
        return r.Ichimoku.Signal switch
        {
            IchimokuSignal.BelowCloud => new(Zone.Accumulate, "Below Ichimoku cloud"),
            IchimokuSignal.AboveCloud => new(Zone.Distribute, "Above Ichimoku cloud"),
            _                         => new(Zone.Hold,        "Inside Ichimoku cloud"),
        };
    }
}
