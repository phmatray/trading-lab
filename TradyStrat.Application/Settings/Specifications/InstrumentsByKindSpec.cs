using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.Settings.Specifications;

public sealed class InstrumentsByKindSpec : Specification<Instrument>
{
    public InstrumentsByKindSpec(InstrumentKind kind)
    {
        Query.Where(i => i.Kind == kind).OrderBy(i => i.Ticker);
    }
}
