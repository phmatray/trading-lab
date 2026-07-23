using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.MarketData;

public sealed class NoTradingDaysException(string message = "No trading days available for the focus ticker.")
    : TradyStratException(message);
