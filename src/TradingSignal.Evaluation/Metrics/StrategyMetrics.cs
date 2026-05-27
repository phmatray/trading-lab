namespace TradingSignal.Evaluation.Metrics;

public sealed record StrategyMetrics(
    string Label,
    PredictionScores Predictions,
    ReturnSeriesMetrics Returns,
    int TradeCount);
