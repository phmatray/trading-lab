namespace TradingStrat.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting a cash withdrawal that exceeds the available cash balance.
/// </summary>
public class InsufficientCashException : DomainException
{
    public decimal AvailableCash { get; }
    public decimal RequiredAmount { get; }

    public InsufficientCashException(decimal availableCash, decimal requiredAmount)
        : base($"Insufficient cash. Available: {availableCash:C}, Required: {requiredAmount:C}")
    {
        AvailableCash = availableCash;
        RequiredAmount = requiredAmount;
    }
}
