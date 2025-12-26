namespace TradingStrat.Domain.Entities;

/// <summary>
/// Represents a single backtest execution that has been saved to the archive.
/// This entity stores the complete configuration and results of a backtest run for historical reference.
/// </summary>
public class BacktestRun
{
    /// <summary>
    /// Unique identifier for the backtest run.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The ticker symbol that was backtested (e.g., "AAPL", "MSFT").
    /// </summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>
    /// The type of strategy used (e.g., "ma", "rsi", "macd", "ml", "custom").
    /// </summary>
    public string StrategyType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the strategy (e.g., "MA Crossover (5/20)", "RSI 14").
    /// </summary>
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized strategy parameters.
    /// Example: {"FastPeriod": 5, "SlowPeriod": 20} for MA Crossover
    /// </summary>
    public string StrategyParametersJson { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized backtest configuration (ticker, dates, capital, commissions, etc.).
    /// </summary>
    public string ConfigJson { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized backtest results (metrics, trades, equity curve, etc.).
    /// </summary>
    public string ResultsJson { get; set; } = string.Empty;

    /// <summary>
    /// When the backtest was executed (UTC).
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// How long the backtest took to execute (in milliseconds).
    /// </summary>
    public int ExecutionTimeMs { get; set; }

    /// <summary>
    /// Execution status: "Success", "Failed", "Cancelled".
    /// </summary>
    public string Status { get; set; } = "Success";

    /// <summary>
    /// Error message if the backtest failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Optional comma-separated tags for categorization (e.g., "momentum,short-term").
    /// </summary>
    public string? Tags { get; set; }
}
