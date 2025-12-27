using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Command for cash transactions (deposit or withdrawal).
/// Validates all parameters to ensure only valid commands can be created.
/// </summary>
public record CashTransactionCommand
{
    public int PortfolioId { get; init; }
    public TransactionType Type { get; init; }
    public decimal Amount { get; init; }
    public string? Notes { get; init; }

    public CashTransactionCommand(
        int PortfolioId,
        TransactionType Type,
        decimal Amount,
        string? Notes = null)
    {
        // Validate parameters
        ValidationGuard.Require(PortfolioId).GreaterThan(0, "Portfolio ID must be positive");
        ValidationGuard.Require(Amount).GreaterThan(0m, "Transaction amount must be positive");

        // Assign validated values
        this.PortfolioId = PortfolioId;
        this.Type = Type;
        this.Amount = Amount;
        this.Notes = Notes;
    }
}

/// <summary>
/// Inbound port (use case interface) for managing portfolio cash.
/// </summary>
public interface IManageCashUseCase
{
    /// <summary>
    /// Executes a cash transaction (deposit or withdrawal).
    /// </summary>
    /// <param name="command">The cash transaction command.</param>
    /// <returns>Result indicating success or failure with errors.</returns>
    Task<Result<bool>> ExecuteAsync(CashTransactionCommand command);

    /// <summary>
    /// Gets the cash transaction history for a portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <returns>Result containing the list of cash transactions, or errors if the operation failed.</returns>
    Task<Result<List<PortfolioCashTransaction>>> GetTransactionHistoryAsync(int portfolioId);
}
