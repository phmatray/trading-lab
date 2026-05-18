using TradyStrat.Domain;

namespace TradyStrat.Application.Indicators.Zones;

public interface IZoneRule
{
    string Name { get; }
    ZoneVote? Apply(decimal price, IndicatorBundle r);
}
