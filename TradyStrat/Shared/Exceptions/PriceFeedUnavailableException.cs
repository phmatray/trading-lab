namespace TradyStrat.Shared.Exceptions;

public sealed class PriceFeedUnavailableException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
