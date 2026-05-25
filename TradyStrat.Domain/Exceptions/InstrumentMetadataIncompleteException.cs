using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Exceptions;

public sealed class InstrumentMetadataIncompleteException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
