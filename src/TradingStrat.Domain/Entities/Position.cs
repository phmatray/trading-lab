namespace TradingStrat.Domain.Entities;

/// <summary>
/// Represents an investment position in a portfolio.
/// </summary>
public class Position
{
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
    /// </summary>
    public required string Ticker { get; set; }

    /// <summary>
    /// Gets or sets the number of shares held.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the average entry price per share.
    /// </summary>
    public decimal EntryPrice { get; set; }

    /// <summary>
    /// Gets or sets the date when the position was entered.
    /// </summary>
    public DateTime EntryDate { get; set; }

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
