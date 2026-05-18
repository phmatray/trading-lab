using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.Fx.Providers;
using TradyStrat.Features.PriceFeed.Providers;

namespace TradyStrat.Features.Settings.UseCases;

public sealed record ProbeInstrumentInput(string Ticker);

public sealed class ProbeInstrumentUseCase(
    IPriceFeed priceFeed,
    IFxRateProvider fx,
    ILogger<ProbeInstrumentUseCase> log)
    : UseCaseBase<ProbeInstrumentInput, InstrumentMetadata>(log)
{
    protected override async Task<InstrumentMetadata> ExecuteCore(
        ProbeInstrumentInput input, CancellationToken ct)
    {
        var ticker = (input.Ticker ?? "").Trim().ToUpperInvariant();
        if (ticker.Length == 0)
            throw new InstrumentNotFoundException("Ticker must not be empty.");

        var meta = await priceFeed.GetInstrumentMetadataAsync(ticker, ct);

        // FX-pair sanity check — surface unsupported currencies before commit.
        if (!string.Equals(meta.Currency, "EUR", StringComparison.OrdinalIgnoreCase))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            try
            {
                _ = await fx.FetchAsync("EUR", meta.Currency, today.AddDays(-1), today, ct);
            }
            catch (FxRateUnavailableException ex)
            {
                throw new UnsupportedCurrencyException(
                    $"EUR/{meta.Currency} FX rate is not available from Yahoo.", ex);
            }
        }

        return meta;
    }
}
