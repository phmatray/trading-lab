using TradyStrat.Application.Fx.Providers;
using TradyStrat.Application.PriceFeed.Providers;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.MarketData;
using TradyStrat.Domain.Shared.Money;
using TradyStrat.Domain.Shared.Market;

namespace TradyStrat.Application.Settings.UseCases;

public sealed record ProbeInstrumentInput(string Ticker, InstrumentKind Kind);

public sealed class ProbeInstrumentUseCase(
    IPriceFeed priceFeed,
    IFxRateProvider fx,
    IClock clock,
    ILogger<ProbeInstrumentUseCase> log)
    : UseCaseBase<ProbeInstrumentInput, Instrument>(log)
{
    protected override async Task<Instrument> ExecuteCore(
        ProbeInstrumentInput input, CancellationToken ct)
    {
        var ticker = (input.Ticker ?? "").Trim().ToUpperInvariant();
        if (ticker.Length == 0)
            throw new InstrumentNotFoundException("Ticker must not be empty.");

        var probed = await priceFeed.ProbeAsync(ticker, ct);

        // FX-pair sanity check — surface unsupported currencies before commit.
        if (probed.Currency != Currency.Eur)
        {
            var today = DateOnly.FromDateTime(clock.UtcNow());
            try
            {
                _ = await fx.FetchAsync(
                    "EUR", probed.Currency.Code, today.AddDays(-1), today, ct);
            }
            catch (FxRateUnavailableException ex)
            {
                throw new UnsupportedCurrencyException(
                    $"EUR/{probed.Currency.Code} FX rate is not available from Yahoo.", ex);
            }
        }

        // The probe defaulted Kind to Held; re-stamp from the input. Confirm
        // happens later in AddInstrumentUseCase.
        if (probed.Kind != input.Kind)
            probed = Instrument.Probed(
                ticker:     probed.Ticker,
                name:       probed.Name,
                currency:   probed.Currency,
                exchange:   probed.Exchange,
                timezoneId: probed.Timezone,
                kind:       input.Kind,
                now:        clock.UtcNow());

        return probed;
    }
}
