namespace TradingStrat.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to add a position with a ticker that already exists in the portfolio.
/// </summary>
public class DuplicatePositionException : DomainException
{
    public string Ticker { get; }

    public DuplicatePositionException(string ticker)
        : base($"A position with ticker '{ticker}' already exists in this portfolio.")
    {
        Ticker = ticker;
    }
}
