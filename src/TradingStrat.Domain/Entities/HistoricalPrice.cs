using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Entities;

/// <summary>
/// Domain entity representing a single bar of historical price data (OHLCV - Open, High, Low, Close, Volume).
/// Used for both backtesting and live analysis.
/// Maps to the HistoricalPrices table in the SQLite database.
/// Supports multiple timeframes (M1, M5, M15, M30, H1, H4, D1, W1, MN1).
/// </summary>
public class HistoricalPrice
{
    /// <summary>
    /// Database primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Stock ticker symbol (e.g., "CON3.L" for London Stock Exchange).
    /// Required field.
    /// </summary>
    public required string Ticker { get; set; }

    /// <summary>
    /// Optional ISIN (International Securities Identification Number) code.
    /// Used for ticker resolution when fetching data.
    /// </summary>
    public string? ISIN { get; set; }

    /// <summary>
    /// Timeframe for this price bar (M1, M5, M15, M30, H1, H4, D1, W1, MN1).
    /// Defaults to D1 (daily) for backward compatibility.
    /// Combined with Ticker and DateTime to form unique constraint in database.
    /// </summary>
    public TimeFrameUnit TimeFrame { get; set; } = TimeFrameUnit.D1;

    /// <summary>
    /// Date and time for this price bar.
    /// The granularity depends on the TimeFrame property.
    /// </summary>
    public DateTime DateTime { get; set; }

    /// <summary>
    /// Opening price for the period.
    /// </summary>
    public decimal? Open { get; set; }

    /// <summary>
    /// Highest price during the period.
    /// </summary>
    public decimal? High { get; set; }

    /// <summary>
    /// Lowest price during the period.
    /// </summary>
    public decimal? Low { get; set; }

    /// <summary>
    /// Closing price for the period (typically used for technical indicators).
    /// </summary>
    public decimal? Close { get; set; }

    /// <summary>
    /// Adjusted closing price accounting for splits and dividends.
    /// Used for accurate historical performance calculations.
    /// </summary>
    public decimal? AdjustedClose { get; set; }

    /// <summary>
    /// Trading volume (number of shares traded).
    /// </summary>
    public long? Volume { get; set; }

    /// <summary>
    /// Timestamp when this record was created in the database.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
