using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Exceptions;

public sealed class CurrencyMismatchException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
