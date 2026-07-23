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

    // Domain Behaviors

    /// <summary>
    /// Calculates the total cost basis of this position (Quantity × Entry Price).
    /// </summary>
    public decimal CalculateCostBasis() => Quantity * EntryPrice;

    /// <summary>
    /// Calculates the current market value of this position (Quantity × Current Price).
    /// </summary>
    /// <param name="currentPrice">The current market price per share.</param>
    public decimal CalculateMarketValue(decimal currentPrice)
    {
        ValidationGuard.Require(currentPrice).GreaterThanOrEqual(0m, "Current price cannot be negative");
        return Quantity * currentPrice;
    }

    /// <summary>
    /// Calculates the unrealized gain or loss for this position.
    /// </summary>
    /// <param name="currentPrice">The current market price per share.</param>
    /// <returns>Positive value for gains, negative for losses.</returns>
    public decimal CalculateUnrealizedGainLoss(decimal currentPrice)
    {
        decimal marketValue = CalculateMarketValue(currentPrice);
        decimal costBasis = CalculateCostBasis();
        return marketValue - costBasis;
    }

    /// <summary>
    /// Calculates the gain or loss percentage for this position.
    /// </summary>
    /// <param name="currentPrice">The current market price per share.</param>
    /// <returns>Percentage gain (positive) or loss (negative).</returns>
    public decimal CalculateGainLossPercentage(decimal currentPrice)
    {
        decimal gainLoss = CalculateUnrealizedGainLoss(currentPrice);
        decimal costBasis = CalculateCostBasis();
        return costBasis > 0 ? (gainLoss / costBasis) * 100 : 0;
    }

    /// <summary>
    /// Calculates the allocation percentage of this position within a total portfolio value.
    /// </summary>
    /// <param name="currentPrice">The current market price per share.</param>
    /// <param name="totalPortfolioValue">The total portfolio value including cash and all positions.</param>
    /// <returns>Percentage of portfolio allocated to this position.</returns>
    public decimal CalculateAllocationPercentage(decimal currentPrice, decimal totalPortfolioValue)
    {
        ValidationGuard.Require(totalPortfolioValue).GreaterThanOrEqual(0m, "Total portfolio value cannot be negative");

        if (totalPortfolioValue == 0)
        {
            return 0;
        }

        decimal marketValue = CalculateMarketValue(currentPrice);
        return (marketValue / totalPortfolioValue) * 100;
    }

    /// <summary>
    /// Returns true if this position is currently profitable at the given price.
    /// </summary>
    /// <param name="currentPrice">The current market price per share.</param>
    public bool IsProfitable(decimal currentPrice) => CalculateUnrealizedGainLoss(currentPrice) > 0;

    /// <summary>
    /// Returns true if this position is currently at a loss at the given price.
    /// </summary>
    /// <param name="currentPrice">The current market price per share.</param>
    public bool IsAtLoss(decimal currentPrice) => CalculateUnrealizedGainLoss(currentPrice) < 0;

    /// <summary>
    /// Returns true if this position is currently break-even at the given price.
    /// </summary>
    /// <param name="currentPrice">The current market price per share.</param>
    public bool IsBreakEven(decimal currentPrice) => CalculateUnrealizedGainLoss(currentPrice) == 0;

    /// <summary>
    /// Gets the number of days this position has been held.
    /// </summary>
    public int GetHoldingPeriodDays() => (DateTime.Today - EntryDate).Days;

    /// <summary>
    /// Gets the holding period as a TimeSpan.
    /// </summary>
    public TimeSpan GetHoldingPeriod() => DateTime.Today - EntryDate;

    /// <summary>
    /// Returns true if the position is considered a long-term hold (>= 1 year).
    /// </summary>
    public bool IsLongTerm() => GetHoldingPeriodDays() >= 365;

    /// <summary>
    /// Returns true if the position is considered a short-term hold (&lt; 1 year).
    /// </summary>
    public bool IsShortTerm() => GetHoldingPeriodDays() < 365;

    public override string ToString() =>
        $"{Ticker}: {Quantity} shares @ {EntryPrice:C2} (Cost Basis: {CalculateCostBasis():C2})";
}
