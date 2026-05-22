using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Application.Fx;

public sealed class FxConverter(IFxRateReadRepository rates)
{
    public async Task<decimal> ToEurAsync(
        decimal amount, string fromCurrency, DateOnly asOf, CancellationToken ct)
    {
        var ccy = (fromCurrency ?? "").Trim().ToUpperInvariant();
        if (ccy.Length == 0)
            throw new FxRateUnavailableException("Currency must not be empty.");
        if (ccy == "EUR") return amount;

        var fx = await rates.LatestAsync("EUR", ccy, asOf, ct)
            ?? throw new FxRateUnavailableException(
                $"No EUR/{ccy} rate on or before {asOf:yyyy-MM-dd}");
        // Rate = Quote per 1 Base. With Base=EUR, Quote=ccy:
        //   to convert N ccy to EUR -> N / Rate.
        return amount / fx.Rate;
    }
}
