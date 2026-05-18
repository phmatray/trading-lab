namespace TradyStrat.Domain.Exceptions;

public sealed class IndicatorComputationException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
