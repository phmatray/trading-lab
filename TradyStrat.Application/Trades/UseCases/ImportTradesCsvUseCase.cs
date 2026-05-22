using Ardalis.Specification;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Application.Settings.Specifications;
using TradyStrat.Application.Time;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Application.Trades.UseCases;

public sealed record ImportTradesCsvInput(string CsvText);
public sealed record ImportTradesCsvResult(int RowsImported);

public sealed class ImportTradesCsvUseCase(
    IPortfolioRepository portfolios,
    IReadRepositoryBase<Instrument> instruments,
    IClock clock,
    ISettingsReader settings,
    ILogger<ImportTradesCsvUseCase> log)
    : UseCaseBase<ImportTradesCsvInput, ImportTradesCsvResult>(log)
{
    // CSV import currently targets the configured focus instrument; per-row Ticker
    // column support is a follow-up.
    protected override async Task<ImportTradesCsvResult> ExecuteCore(
        ImportTradesCsvInput input, CancellationToken ct)
    {
        var focusTicker = await settings.FocusTickerAsync(ct);
        var focus = await instruments.FirstOrDefaultAsync(
            new InstrumentByTickerSpec(focusTicker), ct)
            ?? throw new CsvImportException(
                $"Focus instrument '{focusTicker}' is not registered.");

        var focusCurrency = Currency.Parse(focus.Currency);
        var rows = CsvImportService.Parse(new StringReader(input.CsvText));
        var now  = clock.UtcNow();

        var drafts = rows.Select(r => new TradeDraft(
            new InstrumentId(focus.Id),
            r.ExecutedOn, r.Side,
            Quantity.Of(r.Quantity),
            Price.Of(Money.Of(r.PricePerShare, focusCurrency)),
            Money.Of(r.FeesEur, Currency.Eur),
            r.Note ?? "")).ToList();

        var portfolio = await portfolios.GetAsync(ct);
        var results = portfolio.ImportTrades(drafts, now);
        await portfolios.SaveAsync(portfolio, ct);

        return new ImportTradesCsvResult(results.Count);
    }
}
