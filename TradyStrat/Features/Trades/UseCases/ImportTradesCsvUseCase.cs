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
    ILogger<ImportTradesCsvUseCase> log)
    : UseCaseBase<ImportTradesCsvInput, ImportTradesCsvResult>(log)
{
    // Phase 1 CSV import targets the focus instrument; the per-row Ticker
    // column lands with the dropdown work in Task 15.
    private const string FocusTicker = "CON3.L";

    protected override async Task<ImportTradesCsvResult> ExecuteCore(
        ImportTradesCsvInput input, CancellationToken ct)
    {
        var rows = CsvImportService.Parse(new StringReader(input.CsvText));
        var now  = clock.UtcNow();

        var focus = await instruments.FirstOrDefaultAsync(
            new InstrumentByTickerSpec(FocusTicker), ct)
            ?? throw new CsvImportException(
                $"Focus instrument '{FocusTicker}' is not registered.");

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
