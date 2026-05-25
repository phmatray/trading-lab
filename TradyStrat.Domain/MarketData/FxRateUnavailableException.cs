using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.MarketData;

public sealed class FxRateUnavailableException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
