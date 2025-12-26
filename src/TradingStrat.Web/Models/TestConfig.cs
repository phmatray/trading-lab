namespace TradingStrat.Web.Models;

/// <summary>
/// Configuration model for backtest execution.
/// </summary>
public class TestConfig
{
    /// <summary>
    /// Ticker symbol to backtest.
    /// </summary>
    public string Ticker { get; set; } = "AAPL";

    /// <summary>
    /// Start date for backtest period.
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.Today.AddYears(-2);

    /// <summary>
    /// End date for backtest period.
    /// </summary>
    public DateTime EndDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Initial capital for backtest.
    /// </summary>
    public decimal InitialCapital { get; set; } = 10000m;

    /// <summary>
    /// Commission percentage per trade.
    /// </summary>
    public decimal CommissionPercentage { get; set; } = 0.1m;

    /// <summary>
    /// Minimum commission per trade.
    /// </summary>
    public decimal MinimumCommission { get; set; } = 1.0m;
}
