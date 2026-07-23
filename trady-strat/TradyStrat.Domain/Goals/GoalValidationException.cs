using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Goals;

public sealed class GoalValidationException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
