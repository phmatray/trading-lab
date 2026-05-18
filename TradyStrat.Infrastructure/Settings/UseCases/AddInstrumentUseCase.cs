using TradyStrat.Application.Settings.Config;
using TradyStrat.Infrastructure.Fx;
using TradyStrat.Infrastructure.PriceFeed;
using Ardalis.Specification;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Application.UseCases;
using TradyStrat.Application.Fx;
using TradyStrat.Application.PriceFeed;
using TradyStrat.Application.Settings.Specifications;

namespace TradyStrat.Infrastructure.Settings.UseCases;

public sealed record AddInstrumentInput(InstrumentMetadata Probe, InstrumentKind Kind);

public sealed partial class AddInstrumentUseCase(
    IRepositoryBase<Instrument> repo,
    DailyPriceCache priceCache,
    DailyFxCache fxCache,
    IClock clock,
    ILogger<AddInstrumentUseCase> log)
    : UseCaseBase<AddInstrumentInput, Instrument>(log)
{
    protected override async Task<Instrument> ExecuteCore(
        AddInstrumentInput input, CancellationToken ct)
    {
        var probe = input.Probe;

        var dup = await repo.FirstOrDefaultAsync(
            new InstrumentByTickerSpec(probe.Ticker), ct);
        if (dup is not null)
            throw new DuplicateInstrumentException(
                $"Instrument '{probe.Ticker}' is already tracked.");

        var entity = new Instrument
        {
            Id = 0,
            Ticker = probe.Ticker,
            Name = probe.Name,
            Currency = probe.Currency,
            Exchange = probe.Exchange,
            TimezoneId = probe.TimezoneId,
            Kind = input.Kind,
            AddedAt = clock.UtcNow(),
        };
        await repo.AddAsync(entity, ct);

        // Best-effort warm. Failures are logged and swallowed — cache self-heals next startup.
        try { await priceCache.EnsureFreshAsync(entity.Ticker, ct); }
        catch (Exception ex) { LogPriceWarmFailed(log, ex, entity.Ticker); }

        // FX-warm — best-effort. Skip for EUR-denominated instruments.
        if (!string.Equals(entity.Currency, "EUR", StringComparison.OrdinalIgnoreCase))
        {
            try { await fxCache.EnsureFreshAsync("EUR", entity.Currency, ct); }
            catch (Exception ex) { LogFxWarmFailed(log, ex, entity.Currency); }
        }

        return entity;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Add-instrument: price warm failed for {Ticker}")]
    private static partial void LogPriceWarmFailed(ILogger logger, Exception ex, string ticker);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Add-instrument: FX warm failed for EUR/{Quote}")]
    private static partial void LogFxWarmFailed(ILogger logger, Exception ex, string quote);
}
