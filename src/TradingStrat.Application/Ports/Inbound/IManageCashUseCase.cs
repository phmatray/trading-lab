using TradingStrat.Application.Commands;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Inbound;

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
