namespace TradyStrat.Features.PredictionMarkets;

public sealed record PredictionMarket(
    string Slug,
    string Question,
    decimal Probability,                 // 0..1, the YES outcome price
    DateOnly EndDate,
    decimal VolumeUsd,
    IReadOnlyList<string> Tags);
