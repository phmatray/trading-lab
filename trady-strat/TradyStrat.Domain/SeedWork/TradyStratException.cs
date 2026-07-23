namespace TradyStrat.Domain.SeedWork;

public abstract class TradyStratException(string message, Exception? inner = null) : Exception(message, inner);
