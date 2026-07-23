namespace TradingSignal.Core;

public sealed record Prediction(
    Guid Id,
    DateTime AsOfUtc,
    string Symbol,
    FeatureSet Features,
    RawSignal Signal,
    int WalkForwardSegment);
