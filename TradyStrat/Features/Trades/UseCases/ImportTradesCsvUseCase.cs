using Ardalis.Specification;
using TradyStrat.Common.UseCases;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.Time;
using TradyStrat.Features.Settings.Specifications;

namespace TradyStrat.Features.Trades.UseCases;

public sealed record ImportTradesCsvInput(string CsvText);
public sealed record ImportTradesCsvResult(int RowsImported);

public sealed class ImportTradesCsvUseCase(
    IRepositoryBase<Trade> repo,
    IReadRepositoryBase<Instrument> instruments,
    IClock clock,
    IConfiguration config,
    ILogger<ImportTradesCsvUseCase> log)
    : UseCaseBase<ImportTradesCsvInput, ImportTradesCsvResult>(log)
{
    // Phase 1 CSV import targets the configured focus instrument; the per-row
    // Ticker column is a separate Phase 2 concern (multi-ticker CSVs).
    protected override async Task<ImportTradesCsvResult> ExecuteCore(
        ImportTradesCsvInput input, CancellationToken ct)
    {
        var focusTicker = config["Tickers:Focus"]
            ?? throw new InvalidOperationException("Tickers:Focus is not configured.");

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
