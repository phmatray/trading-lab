using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Entities;

public record LiveAnalysisResult
{
    public required string Ticker { get; init; }
    public required DateTime AnalysisDateTime { get; init; }
    public required DateTime LatestDataDate { get; init; }

    // Current market data
    public required decimal CurrentPrice { get; init; }
    public required decimal PreviousClose { get; init; }
    public required decimal DailyChange { get; init; }
    public required decimal DailyChangePercent { get; init; }

    // ML Prediction
    public required SignalType PredictedSignal { get; init; }
    public required float PredictedReturn { get; init; }
    public required string PredictionReason { get; init; }

    // Feature values (all 26)
    public required MarketFeatures CurrentFeatures { get; init; }

    // Model metadata
    public required int TrainingDataPoints { get; init; }
    public required DateTime OldestTrainingDate { get; init; }
    public required bool IsDataFresh { get; init; }
    public string? DataFreshnessWarning { get; init; }
}
