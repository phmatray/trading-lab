using TradingStrat.Application.Commands;
using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for managing portfolio cash (deposits and withdrawals).
/// Uses helper method pattern to eliminate try-catch boilerplate.
/// </summary>
public class ManageCashUseCase : IManageCashUseCase
{
    private readonly IPortfolioPort _portfolioPort;

    public ManageCashUseCase(IPortfolioPort portfolioPort)
    {
        _portfolioPort = portfolioPort ?? throw new ArgumentNullException(nameof(portfolioPort));
    }

    /// <inheritdoc />
    public Task<Result<bool>> ExecuteAsync(CashTransactionCommand command)
        => ExecuteWithErrorHandling(() => ExecuteCoreAsync(command), ErrorCodes.Cash.TransactionFailed);

    /// <inheritdoc />
    public Task<Result<List<PortfolioCashTransaction>>> GetTransactionHistoryAsync(int portfolioId)
        => ExecuteWithErrorHandling(() => GetTransactionHistoryCoreAsync(portfolioId), ErrorCodes.Cash.HistoryFailed);

    private static async Task<Result<T>> ExecuteWithErrorHandling<T>(
        Func<Task<Result<T>>> executeCore,
        string errorCode)
    {
        try
        {
            return await executeCore();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Result<T>.Failure(
                Error.NotFound(ex.Message, ErrorCodes.Portfolio.NotFound));
        }
        catch (ArgumentException ex)
        {
            return Result<T>.Failure(
                Error.Validation(ex.Message, $"{errorCode}_VALIDATION"));
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(
                Error.BusinessRule($"Failed: {ex.Message}", $"{errorCode}_FAILED"));
        }
    }

    private async Task<Result<bool>> ExecuteCoreAsync(CashTransactionCommand command)
    {
        // Verify portfolio exists
        Portfolio? portfolio = await _portfolioPort.GetPortfolioByIdAsync(command.PortfolioId);
        if (portfolio == null)
        {
            return Result<bool>.Failure(
                Error.NotFound($"Portfolio {command.PortfolioId} not found", ErrorCodes.Portfolio.NotFound));
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
                            ErrorCodes.Cash.InsufficientFunds));
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

    private async Task<Result<List<PortfolioCashTransaction>>> GetTransactionHistoryCoreAsync(int portfolioId)
    {
        // Verify portfolio exists
        Portfolio? portfolio = await _portfolioPort.GetPortfolioByIdAsync(portfolioId);
        if (portfolio == null)
        {
            return Result<List<PortfolioCashTransaction>>.Failure(
                Error.NotFound($"Portfolio {portfolioId} not found", ErrorCodes.Portfolio.NotFound));
        }

        List<PortfolioCashTransaction> transactions = await _portfolioPort.GetCashTransactionsAsync(portfolioId);
        return Result<List<PortfolioCashTransaction>>.Success(transactions);
    }
}
