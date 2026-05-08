namespace TradyStrat.Features.PredictionMarkets;

public static class PolymarketFilter
{
    public static IReadOnlyList<PredictionMarket> Apply(
        IEnumerable<PredictionMarket> markets,
        DateOnly today,
        decimal minVolumeUsd,
        int maxHorizonDays,
        int maxMarkets)
    {
        var horizon = today.AddDays(maxHorizonDays);
        var seen = new HashSet<string>();
        var deduped = new List<PredictionMarket>();
        foreach (var m in markets)
            if (seen.Add(m.Slug))
                deduped.Add(m);

        return deduped
            .Where(m => m.VolumeUsd >= minVolumeUsd)
            .Where(m => m.EndDate <= horizon)
            .OrderByDescending(m => m.VolumeUsd)
            .Take(maxMarkets)
            .ToList();
    }
}
