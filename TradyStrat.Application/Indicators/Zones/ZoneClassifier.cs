using TradyStrat.Domain;

namespace TradyStrat.Application.Indicators.Zones;

public sealed class ZoneClassifier(IEnumerable<IZoneRule> rules)
{
    public (Zone Zone, IReadOnlyList<string> Reasons) Classify(
        decimal price, IndicatorBundle r)
    {
        var votes = rules
            .Select(rule => rule.Apply(price, r))
            .Where(v => v is not null)
            .Select(v => v!)
            .ToList();

        if (votes.Count == 0) return (Zone.Hold, []);

        var groups = votes
            .GroupBy(v => v.Vote)
            .Select(g => (zone: g.Key, n: g.Count()))
            .OrderByDescending(x => x.n)
            .ToList();

        var majority = groups[0].zone;
        if (groups.Count > 1 && groups[0].n == groups[1].n)
            majority = Zone.Hold;

        return (majority, votes.Select(v => v.Reason).ToList());
    }
}
