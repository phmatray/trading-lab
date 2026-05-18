using TradyStrat.Application.Fx;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.Snapshot.Sections;

public sealed class TickersSection(
    IIndicatorEngine indicators,
    FxConverter fx,
    ListInstrumentsUseCase listInstruments) : ISnapshotSectionProvider
{
    // Preserve legacy iteration order [COIN, BTC-USD] so the Phase 2 sentinel
    // PromptHash stays stable through the Phase 3 refactor.
    private static readonly string[] LegacyWatchlistOrder = ["COIN", "BTC-USD"];

    public int Order => 20;

    public async Task ContributeAsync(SnapshotBuilder builder, int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);
        var primary = instruments.SingleOrDefault(i => i.Id == instrumentId)
            ?? throw new InvalidOperationException(
                $"Instrument id {instrumentId} is not in the Instruments table.");

        var watchlist = instruments
            .Where(i => i.Kind == InstrumentKind.Watchlist)
            .OrderBy(i => Array.IndexOf(LegacyWatchlistOrder, i.Ticker) is var idx && idx < 0
                ? int.MaxValue : idx)
            .ThenBy(i => i.Ticker);
        var catalog = new[] { (primary.Ticker, primary.Currency) }
            .Concat(watchlist.Select(i => (i.Ticker, i.Currency)))
            .ToArray();

        foreach (var (ticker, currency) in catalog)
        {
            var reading = await indicators.ComputeFor(ticker, asOf, ct);
            decimal? eur = null;
            if (!string.Equals(currency, "EUR", StringComparison.OrdinalIgnoreCase))
                eur = await fx.ToEurAsync(reading.Price, currency, asOf, ct);

            builder.Tickers.Add(new TickerContext(
                ticker, currency, reading.Price, eur, reading.Zone, reading.Reasons));
        }
    }
}
