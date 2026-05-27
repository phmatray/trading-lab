namespace TradingSignal.Core;

public sealed record Outcome(
    Guid PredictionId,
    decimal EntryPrice,
    decimal ExitPrice,
    double RealizedReturnPct,
    bool DirectionCorrect);
