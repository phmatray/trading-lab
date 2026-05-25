using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Exceptions;

public sealed class DuplicateInstrumentException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
