using Ardalis.Specification;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Features.Settings.Config;
using TradyStrat.Features.Settings.Specifications;

namespace TradyStrat.Features.Trades.UseCases;

public sealed record ImportTradesCsvInput(string CsvText);
public sealed record ImportTradesCsvResult(int RowsImported);

public sealed class ImportTradesCsvUseCase(
    IRepositoryBase<Trade> repo,
    IReadRepositoryBase<Instrument> instruments,
    IClock clock,
    ISettingsReader settings,
    ILogger<ImportTradesCsvUseCase> log)
    : UseCaseBase<ImportTradesCsvInput, ImportTradesCsvResult>(log)
{
    // Phase 1 CSV import targets the configured focus instrument; the per-row
    // Ticker column is a separate Phase 2 concern (multi-ticker CSVs).
    protected override async Task<ImportTradesCsvResult> ExecuteCore(
        ImportTradesCsvInput input, CancellationToken ct)
    {
        var focusTicker = await settings.FocusTickerAsync(ct);

        var rows = CsvImportService.Parse(new StringReader(input.CsvText));
        var now  = clock.UtcNow();

        var focus = await instruments.FirstOrDefaultAsync(
            new InstrumentByTickerSpec(focusTicker), ct)
            ?? throw new CsvImportException(
                $"Focus instrument '{focusTicker}' is not registered.");

        var trades = rows.Select(r => new Trade
        {
            Id = 0,
            InstrumentId  = focus.Id,
            ExecutedOn    = r.ExecutedOn,
            Side          = r.Side,
            Quantity      = r.Quantity,
            PricePerShare = r.PricePerShare,
            FeesEur       = r.FeesEur,
            Note          = r.Note,
            CreatedAt     = now,
        }).ToList();

        await repo.AddRangeAsync(trades, ct);
        return new ImportTradesCsvResult(trades.Count);
    }
}
