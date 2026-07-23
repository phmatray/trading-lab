namespace TradingStrat.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-specific business rule violations.
/// Allows the application layer to distinguish domain errors from infrastructure or programming errors.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
