namespace TradingSignal.Evaluation.Metrics;

public sealed record ReliabilityBin(
    int Index,
    double Lower,
    double Upper,
    int Count,
    double MeanConfidence,
    double EmpiricalAccuracy);
