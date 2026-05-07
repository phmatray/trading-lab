using Ardalis.Specification;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Settings.Specifications;

public sealed class InstrumentByTickerSpec : Specification<Instrument>
{
    public InstrumentByTickerSpec(string ticker)
    {
        Query.Where(i => i.Ticker == ticker).Take(1);
    }
}
