using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
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

        // FX-pair sanity check — implemented in Plan Task 9 once IFxRateProvider takes (base, quote).
        // Until then, the use case trusts metadata and lets AddInstrumentUseCase warm best-effort.
        _ = fx; // suppress unused-field analyzer warning

        return meta;
    }
}
