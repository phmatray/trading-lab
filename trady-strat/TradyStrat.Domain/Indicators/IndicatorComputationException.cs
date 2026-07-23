using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Indicators;

public sealed class IndicatorComputationException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
