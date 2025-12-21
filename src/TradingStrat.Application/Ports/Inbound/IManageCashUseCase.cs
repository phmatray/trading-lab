using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Command for cash transactions (deposit or withdrawal).
/// </summary>
/// <param name="PortfolioId">The portfolio ID.</param>
/// <param name="Type">Transaction type (Deposit or Withdrawal).</param>
/// <param name="Amount">Transaction amount.</param>
/// <param name="Notes">Optional notes.</param>
public record CashTransactionCommand(
    int PortfolioId,
    TransactionType Type,
    decimal Amount,
    string? Notes
);

/// <summary>
/// Inbound port (use case interface) for managing portfolio cash.
/// </summary>
public interface IManageCashUseCase
{
    /// <summary>
    /// Executes a cash transaction (deposit or withdrawal).
    /// </summary>
    /// <param name="command">The cash transaction command.</param>
    Task ExecuteAsync(CashTransactionCommand command);

    /// <summary>
    /// Gets the cash transaction history for a portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID.</param>
    /// <returns>List of cash transactions.</returns>
    Task<List<PortfolioCashTransaction>> GetTransactionHistoryAsync(int portfolioId);
}
