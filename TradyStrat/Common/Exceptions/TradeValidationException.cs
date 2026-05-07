namespace TradyStrat.Common.Exceptions;

public sealed class TradeValidationException(string message) : TradyStratException(message);
