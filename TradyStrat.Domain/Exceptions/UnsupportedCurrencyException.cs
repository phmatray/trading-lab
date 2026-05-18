namespace TradyStrat.Domain.Exceptions;

public sealed class UnsupportedCurrencyException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
