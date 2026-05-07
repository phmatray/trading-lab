using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Indicators.Zones;

public interface IZoneRule
{
    string Name { get; }
    ZoneVote? Apply(decimal price, IndicatorBundle r);
}
