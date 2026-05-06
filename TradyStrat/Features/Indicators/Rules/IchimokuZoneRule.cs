using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators.Rules;

public sealed class IchimokuZoneRule : IZoneRule
{
    public string Name => "Ichimoku";

    public ZoneVote? Apply(decimal price, IndicatorBundle r) => r.Ichimoku switch
    {
        null => null,
        { Signal: IchimokuSignal.BelowCloud } => new(Zone.Accumulate, "Below Ichimoku cloud"),
        { Signal: IchimokuSignal.AboveCloud } => new(Zone.Distribute, "Above Ichimoku cloud"),
        _                                     => new(Zone.Hold,        "Inside Ichimoku cloud"),
    };
}
