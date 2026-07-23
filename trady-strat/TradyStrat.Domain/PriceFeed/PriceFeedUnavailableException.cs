using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.PriceFeed;

public sealed class PriceFeedUnavailableException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
