using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared.Money;
using TradyStrat.Domain.Shared.Market;

namespace TradyStrat.Domain.Instruments.Events;

/// <summary>
/// Raised by <see cref="Instrument.Probed"/> when a metadata probe (e.g. Yahoo)
/// returns a candidate instrument that has not yet been persisted.
///
/// <para>
/// <b>No InstrumentId on this event by design.</b> A probed instrument carries
/// the zero sentinel (<c>InstrumentId.New()</c>) for its Id until
/// <see cref="Instrument.Confirm"/> stamps <c>AddedAt</c> and the repository
/// persists it. Use <see cref="Ticker"/> as the correlation key on this event;
/// the persisted Id arrives on the subsequent
/// <see cref="InstrumentConfirmed"/> event.
/// </para>
/// </summary>
public sealed record InstrumentProbed(
    string   Ticker,
    Currency Currency,
    Exchange Exchange,
    DateTime OccurredAt) : DomainEvent(OccurredAt);
