using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.SeedWork;

public abstract class AggregateRoot<TId> : Entity<TId> where TId : struct
{
    private readonly List<IDomainEvent> _events = new();

    protected AggregateRoot() { }                    // EF
    protected AggregateRoot(TId id) : base(id) { }

    public IReadOnlyList<IDomainEvent> DomainEvents => _events;

    protected void Raise(IDomainEvent evt) => _events.Add(evt);

    protected static void CheckRule(IBusinessRule rule)
    {
        if (rule.IsBroken())
            throw new BusinessRuleViolationException(rule);
    }

    public IReadOnlyList<IDomainEvent> DequeueDomainEvents()
    {
        var snapshot = _events.ToArray();
        _events.Clear();
        return snapshot;
    }

    /// <summary>
    /// Discards all collected events without dispatching. Intended ONLY for
    /// the multi-step rollback path in <c>Portfolio.ImportTrades</c> — where a
    /// failed batch must leave no orphan events on the AR. No other caller
    /// should invoke this.
    /// </summary>
    internal void ClearDomainEvents() => _events.Clear();
}
