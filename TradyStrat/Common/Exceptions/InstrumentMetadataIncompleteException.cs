namespace TradyStrat.Common.Exceptions;

public sealed class InstrumentMetadataIncompleteException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
