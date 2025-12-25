using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.Entities;

/// <summary>
/// Represents an investment position in a portfolio.
/// </summary>
public class Position
{
    private string _ticker = string.Empty;
    private int _quantity;
    private decimal _entryPrice;
    private DateTime _entryDate;

    /// <summary>
    /// Gets or sets the unique identifier for the position.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the portfolio that owns this position.
    /// </summary>
    public int PortfolioId { get; set; }

    /// <summary>
    /// Gets or sets the ticker symbol for the security.
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
    /// Gets or sets the number of shares held.
    /// Must be greater than zero.
    /// </summary>
    public int Quantity
    {
        get => _quantity;
        set
        {
            ValidationGuard.Require(value).GreaterThan(0);
            _quantity = value;
        }
    }

    /// <summary>
    /// Gets or sets the average entry price per share.
    /// Must be greater than zero.
    /// </summary>
    public decimal EntryPrice
    {
        get => _entryPrice;
        set
        {
            ValidationGuard.Require(value).GreaterThan(0m);
            _entryPrice = value;
        }
    }

    /// <summary>
    /// Gets or sets the date when the position was entered.
    /// Cannot be a future date.
    /// </summary>
    public DateTime EntryDate
    {
        get => _entryDate;
        set
        {
            ValidationGuard.Require(value).LessThanOrEqual(DateTime.Today, "Entry date cannot be in the future");
            _entryDate = value;
        }
    }

    /// <summary>
    /// Gets or sets optional notes about the position.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the position was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the position was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the portfolio that owns this position (navigation property).
    /// </summary>
    public Portfolio Portfolio { get; set; } = null!;
}
