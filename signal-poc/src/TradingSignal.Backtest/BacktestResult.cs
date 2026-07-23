using TradingSignal.Core;

namespace TradingSignal.Backtest;

public sealed record SegmentResult(
    int Segment,
    string StrategyLabel,
    DateTime TestStartUtc,
    DateTime TestEndUtc,
    int PredictionCount,
    int TradeCount,
    IReadOnlyList<double> PerBarReturns,
    IReadOnlyList<double> EquityCurve,
    IReadOnlyList<Prediction> Predictions,
    IReadOnlyList<Outcome> Outcomes,
    IReadOnlyDictionary<string, double> Diagnostics);

public sealed record BacktestResult(
    string Symbol,
    string StrategyLabel,
    IReadOnlyList<SegmentResult> Segments)
{
    public IReadOnlyList<double> ConcatenatedReturns =>
        Segments.SelectMany(s => s.PerBarReturns).ToList();

    public IReadOnlyList<(Prediction Prediction, Outcome Outcome)> AllRecords =>
        Segments.SelectMany(s => s.Predictions.Zip(s.Outcomes)).ToList();

    public int TotalTradeCount => Segments.Sum(s => s.TradeCount);
}
