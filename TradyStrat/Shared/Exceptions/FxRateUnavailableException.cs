namespace TradyStrat.Shared.Exceptions;

public sealed class FxRateUnavailableException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
