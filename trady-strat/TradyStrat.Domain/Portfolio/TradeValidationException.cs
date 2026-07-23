using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Portfolio;

public sealed class TradeValidationException(string message) : TradyStratException(message);
