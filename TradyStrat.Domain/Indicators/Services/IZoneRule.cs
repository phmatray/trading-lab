using TradyStrat.Domain;

namespace TradyStrat.Domain.Indicators.Services;

public interface IZoneRule
{
    string Name { get; }
    ZoneVote? Apply(decimal price, IndicatorBundle r);
}
