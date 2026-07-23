using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Events;

/// <summary>
/// Domain event raised when a trading strategy generates a trading signal.
/// </summary>
/// <param name="StrategyName">The name of the strategy that generated the signal.</param>
/// <param name="Ticker">The ticker symbol for which the signal was generated.</param>
/// <param name="Signal">The trading signal (Buy/Sell/Hold).</param>
/// <param name="Timestamp">The timestamp when the signal was generated.</param>
public record StrategySignalGeneratedEvent(
    string StrategyName,
    string Ticker,
    TradeSignal Signal,
    DateTime Timestamp) : DomainEvent;
