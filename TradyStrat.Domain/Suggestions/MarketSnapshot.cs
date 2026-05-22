namespace TradyStrat.Domain.Suggestions;

public sealed record MarketSnapshot(
    IReadOnlyList<PredictionMarket> Markets,
    IReadOnlyList<MarketCitation> Cited)
{
    public static readonly MarketSnapshot Empty = new([], []);
    public bool IsEmpty => Markets.Count == 0 && Cited.Count == 0;
}
