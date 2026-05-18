using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.Settings.Specifications;

public sealed class InstrumentByTickerSpec : Specification<Instrument>
{
    public InstrumentByTickerSpec(string ticker)
    {
        Query.Where(i => i.Ticker == ticker).Take(1);
    }
}
