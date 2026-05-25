using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Exceptions;

public sealed class BusinessRuleViolationException(IBusinessRule rule)
    : TradyStratException(rule.Message)
{
    public IBusinessRule BrokenRule { get; } = rule;
}
