using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Services;

/// <summary>
/// Abstraction for machine learning prediction services.
/// Allows the Domain layer to remain pure while delegating ML.NET infrastructure concerns to the Infrastructure layer.
/// </summary>
public interface IMLPredictionService
{
    /// <summary>
    /// Trains the ML model using historical market features.
    /// </summary>
    /// <param name="features">Array of market features with target values (NextDayReturn)</param>
    /// <param name="currentIndex">Current bar index (used for walk-forward validation)</param>
    void Train(MarketFeatures[] features, int currentIndex);

    /// <summary>
    /// Predicts the next-day return for the given market features.
    /// </summary>
    /// <param name="features">Current bar's market features</param>
    /// <returns>Predicted next-day return (as decimal percentage, e.g., 0.01 = 1%)</returns>
    decimal Predict(MarketFeatures features);
}
