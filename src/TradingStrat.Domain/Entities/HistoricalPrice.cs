using TradingStrat.Domain.Common;
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
    private string _ticker = string.Empty;
    private decimal? _high;
    private decimal? _low;
    private decimal? _close;
    private long? _volume;

    /// <summary>
    /// Database primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Stock ticker symbol (e.g., "CON3.L" for London Stock Exchange).
    /// Must not be null, empty, or whitespace.
    /// </summary>
    public required string Ticker
    {
        get => _ticker;
        set
        {
            ValidationGuard.Require(value).NotNullOrWhiteSpace();
            _ticker = value;
        }
    }

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
    /// Must be greater than or equal to Low when both are set.
    /// </summary>
    public decimal? High
    {
        get => _high;
        set
        {
            if (value.HasValue && _low.HasValue && value.Value < _low.Value)
            {
                throw new ArgumentException("High price must be greater than or equal to Low price", nameof(High));
            }
            _high = value;
            ValidateCloseWithinRange();
        }
    }

    /// <summary>
    /// Lowest price during the period.
    /// Must be less than or equal to High when both are set.
    /// </summary>
    public decimal? Low
    {
        get => _low;
        set
        {
            if (value.HasValue && _high.HasValue && value.Value > _high.Value)
            {
                throw new ArgumentException("Low price must be less than or equal to High price", nameof(Low));
            }
            _low = value;
            ValidateCloseWithinRange();
        }
    }

    /// <summary>
    /// Closing price for the period (typically used for technical indicators).
    /// Must be between Low and High when all are set.
    /// </summary>
    public decimal? Close
    {
        get => _close;
        set
        {
            _close = value;
            ValidateCloseWithinRange();
        }
    }

    /// <summary>
    /// Adjusted closing price accounting for splits and dividends.
    /// Used for accurate historical performance calculations.
    /// </summary>
    public decimal? AdjustedClose { get; set; }

    /// <summary>
    /// Trading volume (number of shares traded).
    /// Must be non-negative.
    /// </summary>
    public long? Volume
    {
        get => _volume;
        set
        {
            if (value.HasValue)
            {
                ValidationGuard.Require(value.Value).GreaterThanOrEqual(0L);
            }
            _volume = value;
        }
    }

    private void ValidateCloseWithinRange()
    {
        if (_close.HasValue && _low.HasValue && _high.HasValue)
        {
            if (_close.Value < _low.Value || _close.Value > _high.Value)
            {
                throw new ArgumentException("Close price must be between Low and High prices", nameof(Close));
            }
        }
    }

    /// <summary>
    /// Timestamp when this record was created in the database.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
