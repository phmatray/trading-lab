using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Instruments.Events;

public sealed record InstrumentConfirmed(InstrumentId InstrumentId, DateTime OccurredAt) : DomainEvent(OccurredAt);
