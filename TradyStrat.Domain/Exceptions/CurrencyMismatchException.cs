namespace TradyStrat.Domain.Exceptions;

public sealed class CurrencyMismatchException(string message) : TradyStratException(message);
