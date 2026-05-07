namespace TradyStrat.Features.PredictionMarkets;

public sealed record MarketCitation(
    string Slug,                         // FK back into MarketSnapshot.Markets
    string Claim);                       // one-line reason AI weighed this market
