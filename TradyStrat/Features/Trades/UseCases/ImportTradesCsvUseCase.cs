using Ardalis.Specification;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.Trades;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Time;

namespace TradyStrat.Features.Trades.UseCases;

public sealed record ImportTradesCsvInput(string CsvText);
public sealed record ImportTradesCsvResult(int RowsImported);

public sealed class ImportTradesCsvUseCase(
    IRepositoryBase<Trade> repo, IClock clock,
    ILogger<ImportTradesCsvUseCase> log)
    : UseCaseBase<ImportTradesCsvInput, ImportTradesCsvResult>(log)
{
    protected override async Task<ImportTradesCsvResult> ExecuteCore(
        ImportTradesCsvInput input, CancellationToken ct)
    {
        var rows = CsvImportService.Parse(new StringReader(input.CsvText));
        var now  = clock.UtcNow();
        var trades = rows.Select(r => new Trade
        {
            Id = 0,
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
