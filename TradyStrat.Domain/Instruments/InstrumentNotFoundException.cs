using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Instruments;

public sealed class InstrumentNotFoundException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
