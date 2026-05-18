using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.Settings.Specifications;

public sealed class AllInstrumentsSpec : Specification<Instrument>
{
    public AllInstrumentsSpec()
    {
        Query.OrderBy(i => i.Ticker);
    }
}
