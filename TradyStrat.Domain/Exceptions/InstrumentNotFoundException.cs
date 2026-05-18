namespace TradyStrat.Domain.Exceptions;

public sealed class InstrumentNotFoundException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
