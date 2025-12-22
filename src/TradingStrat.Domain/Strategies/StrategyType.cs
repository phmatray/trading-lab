namespace TradingStrat.Domain.Strategies;

/// <summary>
/// Enumeration of all available trading strategy types.
/// Provides compile-time safety for strategy selection.
/// </summary>
public enum StrategyType
{
    /// <summary>
    /// Moving Average Crossover strategy - Buy when fast SMA crosses above slow SMA
    /// </summary>
    MovingAverageCrossover,

    /// <summary>
    /// Relative Strength Index strategy - Mean-reversion with overbought/oversold levels
    /// </summary>
    RSI,

    /// <summary>
    /// MACD strategy - Moving Average Convergence Divergence crossover
    /// </summary>
    MACD,

    /// <summary>
    /// Machine Learning strategy using FastTree gradient boosting
    /// </summary>
    MachineLearning,

    /// <summary>
    /// Ichimoku Cloud strategy - Multi-timeframe trend-following system
    /// </summary>
    Ichimoku
}
