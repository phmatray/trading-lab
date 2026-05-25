namespace TradyStrat.Domain.SeedWork;

public abstract record DomainEvent : IDomainEvent
{
    public Guid     EventId    { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; }
}
