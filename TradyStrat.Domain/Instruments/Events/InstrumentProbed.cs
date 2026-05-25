using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Instruments.Events;

public sealed record InstrumentProbed(
    string   Ticker,
    Currency Currency,
    Exchange Exchange,
    DateTime OccurredAt) : DomainEvent(OccurredAt);
