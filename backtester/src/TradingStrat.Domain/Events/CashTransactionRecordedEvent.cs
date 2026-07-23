using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.Events;

/// <summary>
/// Domain event raised when a cash transaction (deposit or withdrawal) is recorded in a portfolio.
/// </summary>
/// <param name="PortfolioId">The unique identifier of the portfolio.</param>
/// <param name="Type">The type of transaction (Deposit or Withdrawal).</param>
/// <param name="Amount">The amount of the transaction.</param>
/// <param name="TransactionDate">The date of the transaction.</param>
public record CashTransactionRecordedEvent(
    int PortfolioId,
    TransactionType Type,
    decimal Amount,
    DateTime TransactionDate) : DomainEvent;
