using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Instruments.Events;

public sealed record InstrumentConfirmed(InstrumentId InstrumentId, DateTime OccurredAt) : DomainEvent(OccurredAt);
