using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Exceptions;

public sealed class TradeValidationException(string message) : TradyStratException(message);
