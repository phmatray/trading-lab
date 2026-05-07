namespace TradyStrat.Common.Exceptions;

public sealed class FxRateUnavailableException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
