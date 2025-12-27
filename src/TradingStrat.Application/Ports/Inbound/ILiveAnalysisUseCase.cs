using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for analyzing current market positions using machine learning predictions.
/// Trains an ML model on historical data and predicts next-day return for the current position.
/// This use case eliminates the reflection hack from the original architecture by using
/// IIndicatorCalculator directly instead of accessing MachineLearningStrategy's private fields.
/// Extracted from ProgramAnalyze.RunAsync in original architecture.
/// </summary>
public interface ILiveAnalysisUseCase
{
    /// <summary>
    /// Executes live analysis workflow: fetches latest data, trains ML model, and predicts next-day return.
    /// </summary>
    /// <param name="command">Command containing ticker, prediction thresholds, and data refresh settings.</param>
    /// <param name="progress">Optional progress reporter for UI updates.</param>
    /// <returns>Analysis result containing prediction, signal recommendation, and market features.</returns>
    Task<LiveAnalysisResult> ExecuteAsync(
        AnalysisCommand command,
        IProgress<string>? progress = null);
}

/// <summary>
/// Command object for live market position analysis.
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record AnalysisCommand
{
    public string Ticker { get; init; }
    public PredictionThresholds? Thresholds { get; init; }
    public bool FetchFreshData { get; init; }
    public TimeFrame? TimeFrame { get; init; }

    public AnalysisCommand(
        string Ticker,
        PredictionThresholds? Thresholds = null,
        bool FetchFreshData = true,
        TimeFrame? TimeFrame = null)
    {
        // Validate parameters
        ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();

        // Assign validated values
        this.Ticker = Ticker.ToUpperInvariant().Trim();
        this.Thresholds = Thresholds;
        this.FetchFreshData = FetchFreshData;
        this.TimeFrame = TimeFrame;
    }
}
