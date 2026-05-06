namespace TradyStrat.Shared.Exceptions;

public abstract class TradyStratException(string message, Exception? inner = null) : Exception(message, inner);
