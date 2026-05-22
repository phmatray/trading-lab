using TradyStrat.Application.Settings;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Fx;
using TradyStrat.Infrastructure.PriceFeed;

namespace TradyStrat.Infrastructure.Settings.UseCases;

public sealed record AddInstrumentInput(Instrument Probed);

public sealed partial class AddInstrumentUseCase(
    IInstrumentRepository repo,
    DailyPriceCache priceCache,
    DailyFxCache fxCache,
    IClock clock,
    ILogger<AddInstrumentUseCase> log)
    : UseCaseBase<AddInstrumentInput, Instrument>(log)
{
    protected override async Task<Instrument> ExecuteCore(
        AddInstrumentInput input, CancellationToken ct)
    {
        var instrument = input.Probed;
        instrument.Confirm(clock);

        // Repository enforces ticker uniqueness — throws DuplicateInstrumentException.
        await repo.AddAsync(instrument, ct);

        // Best-effort warm. Failures are logged and swallowed — cache self-heals next startup.
        try { await priceCache.EnsureFreshAsync(instrument.Ticker, ct); }
        catch (Exception ex) { LogPriceWarmFailed(log, ex, instrument.Ticker); }

        // FX-warm — skip for EUR-denominated instruments.
        if (instrument.Currency.Code != "EUR")
        {
            try { await fxCache.EnsureFreshAsync("EUR", instrument.Currency.Code, ct); }
            catch (Exception ex) { LogFxWarmFailed(log, ex, instrument.Currency.Code); }
        }

        return instrument;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Add-instrument: price warm failed for {Ticker}")]
    private static partial void LogPriceWarmFailed(ILogger logger, Exception ex, string ticker);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Add-instrument: FX warm failed for EUR/{Quote}")]
    private static partial void LogFxWarmFailed(ILogger logger, Exception ex, string quote);
}
