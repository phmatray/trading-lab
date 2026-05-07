using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Indicators;

public sealed class RsiZoneRule : IZoneRule
{
    public string Name => "RSI";

    public ZoneVote? Apply(decimal price, IndicatorBundle r) => r.Rsi switch
    {
        null     => null,
        < 30m    => new(Zone.Accumulate, $"RSI(14) {r.Rsi:F0}, oversold"),
        > 70m    => new(Zone.Distribute, $"RSI(14) {r.Rsi:F0}, overbought"),
        _        => new(Zone.Hold,        $"RSI(14) {r.Rsi:F0}, neutral"),
    };
}
