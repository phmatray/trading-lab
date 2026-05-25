namespace TradyStrat.Domain.SeedWork;

public abstract record DomainEvent(DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}
