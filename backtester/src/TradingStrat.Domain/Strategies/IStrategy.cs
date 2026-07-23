using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Strategies;

/// <summary>
/// Defines the contract for trading strategies that generate buy/sell/hold signals.
/// Strategies analyze historical price data and market indicators to make trading decisions.
/// </summary>
public interface IStrategy
{
    /// <summary>
    /// Gets the unique name of this strategy (e.g., "Moving Average Crossover").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a human-readable description of how this strategy works.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Initializes the strategy with historical market data.
    /// Must be called before GenerateSignal can be used.
    /// </summary>
    /// <param name="historicalData">Complete historical price data for the security being traded.</param>
    void Initialize(IReadOnlyList<HistoricalPrice> historicalData);

    /// <summary>
    /// Generates a trading signal (Buy/Sell/Hold) for the current market position.
    /// </summary>
    /// <param name="currentIndex">Index in the historical data representing the current time point.</param>
    /// <param name="currentCash">Available cash in the portfolio.</param>
    /// <param name="currentPosition">Current number of shares held (positive = long, 0 = no position).</param>
    /// <returns>A TradeSignal indicating the action to take and quantity to trade.</returns>
    TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition);

    /// <summary>
    /// Gets the configuration parameters for this strategy.
    /// </summary>
    /// <returns>Dictionary of parameter names and their values (e.g., period lengths, thresholds).</returns>
    Dictionary<string, object> GetParameters();
}
