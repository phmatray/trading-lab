namespace TradyStrat.Application.PredictionMarkets;

public sealed record MarketSnapshot(
    IReadOnlyList<PredictionMarket> Markets,
    IReadOnlyList<MarketCitation> Cited)
{
    public static readonly MarketSnapshot Empty = new([], []);
}
