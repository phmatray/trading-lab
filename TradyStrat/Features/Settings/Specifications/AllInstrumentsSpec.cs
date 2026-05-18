using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Features.Settings.Specifications;

public sealed class AllInstrumentsSpec : Specification<Instrument>
{
    public AllInstrumentsSpec()
    {
        Query.OrderBy(i => i.Ticker);
    }
}
