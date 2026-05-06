namespace TradyStrat.Shared.Exceptions;

public sealed class TradeValidationException(string message) : TradyStratException(message);
