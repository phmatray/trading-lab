using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Shared.Money;

public sealed class UnsupportedCurrencyException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
