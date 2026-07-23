namespace TradingStrat.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to access or modify a position that doesn't exist in the portfolio.
/// </summary>
public class PositionNotFoundException : DomainException
{
    public string Ticker { get; }

    public PositionNotFoundException(string ticker)
        : base($"No position found for ticker '{ticker}' in this portfolio.")
    {
        Ticker = ticker;
    }
}
