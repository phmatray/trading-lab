using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Suggestions;

public sealed class PolymarketUnavailableException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
