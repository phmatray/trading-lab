using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Shared.Money;

public sealed class CurrencyMismatchException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
