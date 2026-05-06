using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators.Rules;

public interface IZoneRule
{
    string Name { get; }
    ZoneVote? Apply(decimal price, IndicatorBundle r);
}
