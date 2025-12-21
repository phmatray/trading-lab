namespace TradingStrat.Domain.Entities;

/// <summary>
/// Transaction type for cash movements.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Cash deposit into the portfolio.
    /// </summary>
    Deposit,

    /// <summary>
    /// Cash withdrawal from the portfolio.
    /// </summary>
    Withdrawal
}

/// <summary>
/// Represents a cash transaction (deposit or withdrawal) in a portfolio.
/// </summary>
public class PortfolioCashTransaction
{
    /// <summary>
    /// Gets or sets the unique identifier for the transaction.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the portfolio for this transaction.
    /// </summary>
    public int PortfolioId { get; set; }

    /// <summary>
    /// Gets or sets the type of transaction (Deposit or Withdrawal).
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Gets or sets the transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the date and time of the transaction.
    /// </summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Gets or sets optional notes about the transaction.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the transaction record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the portfolio for this transaction (navigation property).
    /// </summary>
    public Portfolio Portfolio { get; set; } = null!;
}
