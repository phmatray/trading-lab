using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Suggestions.Events;

/// <summary>
/// Raised by <see cref="Suggestion.From"/> when a new Suggestion AR is constructed.
///
/// <para>
/// <b>Id semantics:</b> <see cref="SuggestionId"/> is the zero sentinel
/// (<c>new SuggestionId(0)</c>) because the event is raised <i>before</i>
/// <c>SaveAsync</c> commits, and EF assigns the real database key on insert via
/// <c>ValueGeneratedOnAdd</c>. Handlers needing the persisted Id must either
/// (a) read it back from the AR after the use case's <c>SaveAsync</c> returns,
/// or (b) wait for a future <c>SuggestionPersisted</c>-style event to be introduced.
/// </para>
/// </summary>
public sealed record SuggestionCreated(
    SuggestionId     SuggestionId,
    InstrumentId     InstrumentId,
    DateOnly         ForDate,
    SuggestionAction Action,
    DateTime         OccurredAt) : DomainEvent(OccurredAt);
