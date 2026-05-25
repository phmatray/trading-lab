namespace TradyStrat.Domain.SeedWork;

public sealed class BusinessRuleViolationException(IBusinessRule rule)
    : TradyStratException(rule.Message)
{
    public IBusinessRule BrokenRule { get; } = rule;
}
