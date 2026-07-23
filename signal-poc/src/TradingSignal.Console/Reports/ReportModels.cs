namespace TradingSignal.ConsoleApp.Reports;

public sealed record StrategyReport(
    string Label,
    int TotalPredictions,
    int NonHoldPredictions,
    int TradeCount,
    double Accuracy,
    double BrierScore,
    double CumulativeReturnPct,
    double AnnualizedSharpe,
    double MaxDrawdownPct,
    IReadOnlyList<SegmentReport> Segments);

public sealed record SegmentReport(
    int Segment,
    string Label,
    DateTime TestStartUtc,
    DateTime TestEndUtc,
    int Predictions,
    int Trades,
    double Accuracy,
    double BrierScore,
    double CumulativeReturnPct,
    double AnnualizedSharpe,
    double MaxDrawdownPct,
    double? SelectedThreshold,
    double? MetaModelTrainAccuracy);

public sealed record RunReport(
    string Symbol,
    string Interval,
    DateTime DataStartUtc,
    DateTime DataEndUtc,
    int CandleCount,
    double FeeBps,
    BuyAndHoldReport BuyAndHold,
    IReadOnlyList<StrategyReport> Strategies);

public sealed record BuyAndHoldReport(
    double CumulativeReturnPct,
    double AnnualizedSharpe,
    double MaxDrawdownPct);
