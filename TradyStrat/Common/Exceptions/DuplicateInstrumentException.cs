namespace TradyStrat.Common.Exceptions;

public sealed class DuplicateInstrumentException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
