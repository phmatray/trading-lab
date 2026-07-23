using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Settings;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Portfolio.Events;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared.Money;
using TradyStrat.Domain.Shared.Market;

namespace TradyStrat.Application.Trades.UseCases;

public sealed record ImportTradesCsvInput(string CsvText);
public sealed record ImportTradesCsvResult(int RowsImported);

public sealed class ImportTradesCsvUseCase(
    IPortfolioRepository portfolios,
    IInstrumentRepository instruments,
    IClock clock,
    IFocusTickerRepository focusTickerRepo,
    IDomainEventDispatcher dispatcher,
    ILogger<ImportTradesCsvUseCase> log)
    : UseCaseBase<ImportTradesCsvInput, ImportTradesCsvResult>(log)
{
    // Rows with a `ticker` column are routed to the matching instrument; rows
    // without one fall back to the configured focus instrument (back-compat).
    protected override async Task<ImportTradesCsvResult> ExecuteCore(
        ImportTradesCsvInput input, CancellationToken ct)
    {
        var focusTicker = (await focusTickerRepo.GetAsync(ct)).Value;
        var focus = await instruments.FindByTickerAsync(focusTicker, ct)
            ?? throw new CsvImportException(
                $"Focus instrument '{focusTicker}' is not registered.");

        var rows = CsvImportService.Parse(new StringReader(input.CsvText));
        var now  = clock.UtcNow();

        var drafts = new List<TradeDraft>(rows.Count);
        var resolved = new Dictionary<string, Instrument>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            var instrument = focus;
            if (r.Ticker is not null && !string.Equals(r.Ticker, focus.Ticker, StringComparison.OrdinalIgnoreCase))
            {
                if (!resolved.TryGetValue(r.Ticker, out var hit))
                {
                    hit = await instruments.FindByTickerAsync(r.Ticker, ct)
                        ?? throw new CsvImportException(
                            $"Instrument '{r.Ticker}' is not registered.");
                    resolved[r.Ticker] = hit;
                }
                instrument = hit;
            }

            drafts.Add(new TradeDraft(
                instrument.Id,
                r.ExecutedOn, r.Side,
                Quantity.Of(r.Quantity),
                Price.Of(Money.Of(r.PricePerShare, instrument.Currency)),
                Money.Of(r.FeesEur, Currency.Eur),
                r.Note ?? ""));
        }

        var portfolio = await portfolios.GetAsync(ct);
        var results = portfolio.ImportTrades(drafts, now);
        var events = await portfolios.SaveAsync(portfolio, ct);
        await dispatcher.DispatchAsync(events, ct);

        return new ImportTradesCsvResult(results.Count);
    }
}
