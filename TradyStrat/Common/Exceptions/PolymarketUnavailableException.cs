namespace TradyStrat.Common.Exceptions;

public sealed class PolymarketUnavailableException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
