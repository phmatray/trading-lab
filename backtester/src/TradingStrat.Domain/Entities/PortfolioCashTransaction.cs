using TradingStrat.Domain.Common;

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
    private int _portfolioId;
    private decimal _amount;
    private DateTime _transactionDate;

    /// <summary>
    /// Gets or sets the unique identifier for the transaction.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the portfolio for this transaction.
    /// Must be greater than zero.
    /// </summary>
    public int PortfolioId
    {
        get => _portfolioId;
        set
        {
            ValidationGuard.Require(value).GreaterThan(0);
            _portfolioId = value;
        }
    }

    /// <summary>
    /// Gets or sets the type of transaction (Deposit or Withdrawal).
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Gets or sets the transaction amount.
    /// Must be greater than zero.
    /// </summary>
    public decimal Amount
    {
        get => _amount;
        set
        {
            ValidationGuard.Require(value).GreaterThan(0m);
            _amount = value;
        }
    }

    /// <summary>
    /// Gets or sets the date and time of the transaction.
    /// Cannot be a future date.
    /// </summary>
    public DateTime TransactionDate
    {
        get => _transactionDate;
        set
        {
            ValidationGuard.Require(value).LessThanOrEqual(DateTime.Today, "Transaction date cannot be in the future");
            _transactionDate = value;
        }
    }

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
