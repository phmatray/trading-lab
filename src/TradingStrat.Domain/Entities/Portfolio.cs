namespace TradingStrat.Domain.Entities;

/// <summary>
/// Portfolio aggregate root representing a collection of investment positions with cash balance.
/// </summary>
public class Portfolio
{
    /// <summary>
    /// Gets or sets the unique identifier for the portfolio.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the portfolio name (e.g., "Growth", "Income", "Retirement").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets an optional description of the portfolio.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the cash balance in the portfolio.
    /// </summary>
    public decimal Cash { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the portfolio was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the portfolio was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the collection of positions in this portfolio.
    /// </summary>
    public List<Position> Positions { get; set; } = new();
}
