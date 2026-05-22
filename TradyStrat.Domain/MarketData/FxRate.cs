using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed class FxRate
{
    public int           Id        { get; private set; }
    public DateOnly      Date      { get; private set; }
    public CurrencyPair  Pair      { get; private set; }
    public decimal       Rate      { get; private set; }    // Quote per 1 Base
    public DateTime      FetchedAt { get; private set; }

    private FxRate() { }   // EF

    public FxRate(DateOnly date, CurrencyPair pair, decimal rate, DateTime fetchedAt)
    {
        Date = date;
        Pair = pair;
        Rate = rate;
        FetchedAt = fetchedAt;
    }
}
