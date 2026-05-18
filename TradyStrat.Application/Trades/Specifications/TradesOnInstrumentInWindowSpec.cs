using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.Trades.Specifications;

/// <summary>
/// Trades on a specific instrument with ExecutedOn in <c>(after, through]</c>.
/// Used by the recent-suggestions section to compute net trade flow in the
/// forward window after each past suggestion.
/// </summary>
public sealed class TradesOnInstrumentInWindowSpec : Specification<Trade>
{
    public TradesOnInstrumentInWindowSpec(int instrumentId, DateOnly afterExclusive, DateOnly throughInclusive)
    {
        Query
            .Where(t => t.InstrumentId == instrumentId
                     && t.ExecutedOn >  afterExclusive
                     && t.ExecutedOn <= throughInclusive)
            .OrderBy(t => t.ExecutedOn);
    }
}
