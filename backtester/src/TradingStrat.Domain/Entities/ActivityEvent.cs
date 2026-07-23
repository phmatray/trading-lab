namespace TradingStrat.Domain.Entities;

/// <summary>
/// Represents a user activity event for the dashboard activity feed.
/// Tracks all significant user actions for audit trail and recent activity display.
/// </summary>
public class ActivityEvent
{
    /// <summary>
    /// Unique identifier for the activity event.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Type of event (e.g., "BacktestRun", "StrategyCreated", "StrategyOptimized", "DataFetched", "PortfolioCreated", "PortfolioRebalanced").
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Optional ID of the related entity (e.g., BacktestRunId, StrategyId, PortfolioId).
    /// </summary>
    public int? EntityId { get; set; }

    /// <summary>
    /// Short title describing the event (e.g., "Backtest RSI 14 on AAPL").
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional detailed description of the event (e.g., "+12.5% return, Sharpe: 1.8").
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// JSON metadata for the event (flexible storage for event-specific data).
    /// Example: {"Ticker": "AAPL", "Return": 12.5, "Sharpe": 1.8}
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When the event occurred (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
