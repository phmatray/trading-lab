using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for managing portfolio cash (deposits and withdrawals).
/// </summary>
public class ManageCashUseCase : IManageCashUseCase
{
    private readonly IPortfolioPort _portfolioPort;

    public ManageCashUseCase(IPortfolioPort portfolioPort)
    {
        _portfolioPort = portfolioPort ?? throw new ArgumentNullException(nameof(portfolioPort));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ExecuteAsync(CashTransactionCommand command)
    {
        // Command validation happens in constructor - command is guaranteed to be valid here

        try
        {
            // Verify portfolio exists
            Portfolio? portfolio = await _portfolioPort.GetPortfolioByIdAsync(command.PortfolioId);
            if (portfolio == null)
            {
                return Result<bool>.Failure(
                    Error.NotFound($"Portfolio {command.PortfolioId} not found", "PORTFOLIO_NOT_FOUND"));
            }

            // Execute transaction based on type
            switch (command.Type)
            {
                case TransactionType.Deposit:
                    await _portfolioPort.AddCashAsync(
                        command.PortfolioId,
                        command.Amount,
                        command.Notes);
                    return Result<bool>.Success(true);

                case TransactionType.Withdrawal:
                    // Check sufficient cash for withdrawal
                    if (portfolio.Cash < command.Amount)
                    {
                        return Result<bool>.Failure(
                            Error.BusinessRule(
                                $"Insufficient cash balance. Available: {portfolio.Cash:C2}, Requested: {command.Amount:C2}",
                                "INSUFFICIENT_CASH"));
                    }

                    await _portfolioPort.WithdrawCashAsync(
                        command.PortfolioId,
                        command.Amount,
                        command.Notes);
                    return Result<bool>.Success(true);

                default:
                    return Result<bool>.Failure(
                        Error.Validation($"Unknown transaction type: {command.Type}", "INVALID_TRANSACTION_TYPE"));
            }
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(
                Error.BusinessRule($"Failed to execute cash transaction: {ex.Message}", "CASH_TRANSACTION_FAILED"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<PortfolioCashTransaction>>> GetTransactionHistoryAsync(int portfolioId)
    {
        try
        {
            // Verify portfolio exists
            Portfolio? portfolio = await _portfolioPort.GetPortfolioByIdAsync(portfolioId);
            if (portfolio == null)
            {
                return Result<List<PortfolioCashTransaction>>.Failure(
                    Error.NotFound($"Portfolio {portfolioId} not found", "PORTFOLIO_NOT_FOUND"));
            }

            List<PortfolioCashTransaction> transactions = await _portfolioPort.GetCashTransactionsAsync(portfolioId);
            return Result<List<PortfolioCashTransaction>>.Success(transactions);
        }
        catch (Exception ex)
        {
            return Result<List<PortfolioCashTransaction>>.Failure(
                Error.BusinessRule($"Failed to retrieve transaction history: {ex.Message}", "TRANSACTION_HISTORY_FAILED"));
        }
    }
}
