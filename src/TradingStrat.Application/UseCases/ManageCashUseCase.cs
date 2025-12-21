using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
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
    public async Task ExecuteAsync(CashTransactionCommand command)
    {
        // Validate input
        if (command.Amount <= 0)
        {
            throw new ArgumentException("Amount must be positive", nameof(command));
        }

        // Verify portfolio exists
        var portfolio = await _portfolioPort.GetPortfolioByIdAsync(command.PortfolioId);
        if (portfolio == null)
        {
            throw new InvalidOperationException($"Portfolio {command.PortfolioId} not found");
        }

        // Execute transaction based on type
        switch (command.Type)
        {
            case TransactionType.Deposit:
                await _portfolioPort.AddCashAsync(
                    command.PortfolioId,
                    command.Amount,
                    command.Notes);
                break;

            case TransactionType.Withdrawal:
                // Check sufficient cash for withdrawal
                if (portfolio.Cash < command.Amount)
                {
                    throw new InvalidOperationException(
                        $"Insufficient cash balance. Available: {portfolio.Cash:C2}, Requested: {command.Amount:C2}");
                }

                await _portfolioPort.WithdrawCashAsync(
                    command.PortfolioId,
                    command.Amount,
                    command.Notes);
                break;

            default:
                throw new ArgumentException($"Unknown transaction type: {command.Type}", nameof(command));
        }
    }

    /// <inheritdoc />
    public async Task<List<PortfolioCashTransaction>> GetTransactionHistoryAsync(int portfolioId)
    {
        return await _portfolioPort.GetCashTransactionsAsync(portfolioId);
    }
}
