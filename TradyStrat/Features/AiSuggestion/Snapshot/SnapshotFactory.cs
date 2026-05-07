using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Time;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Features.Settings.UseCases;
using TradyStrat.Features.Trades.Specifications;

namespace TradyStrat.Features.AiSuggestion.Snapshot;

public sealed class SnapshotFactory(
    IndicatorEngine indicators,
    PortfolioService portfolio,
    FxConverter fx,
    IReadRepositoryBase<GoalConfig> goalRepo,
    IReadRepositoryBase<Trade> tradeRepo,
    ListInstrumentsUseCase listInstruments,
    IConfiguration config,
    IClock clock) : ISnapshotFactory
{
    // Preserve legacy iteration order [COIN, BTC-USD] so the day-one PromptHash
    // remains byte-identical against the pre-multi-ticker fixture. New watchlist
    // instruments fall through to alphabetical order via the ThenBy below.
    private static readonly string[] LegacyWatchlistOrder = ["COIN", "BTC-USD"];

    public async Task<AiSnapshot> CreateAsync(DateOnly asOf, CancellationToken ct)
    {
        var goal = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(clock.UtcNow());

        var focusTicker = config["Tickers:Focus"]
            ?? throw new InvalidOperationException("Tickers:Focus is not configured.");

        var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);
        var focus = instruments.SingleOrDefault(i => i.Ticker == focusTicker)
            ?? throw new InvalidOperationException(
                $"Focus ticker '{focusTicker}' is not in the Instruments table. Add it via Settings.");

        // Catalog order: focus first, then watchlist in legacy order, then any
        // newer watchlist instruments alphabetically.
        var watchlist = instruments
            .Where(i => i.Kind == InstrumentKind.Watchlist)
            .OrderBy(i => Array.IndexOf(LegacyWatchlistOrder, i.Ticker) is var idx && idx < 0
                ? int.MaxValue : idx)
            .ThenBy(i => i.Ticker);
        var catalog = new[] { (focus.Ticker, focus.Currency) }
            .Concat(watchlist.Select(i => (i.Ticker, i.Currency)))
            .ToArray();

        var tickers = new List<TickerContext>();

        foreach (var (ticker, currency) in catalog)
        {
            var reading = await indicators.ComputeFor(ticker, asOf, ct);
            decimal? eur = null;
            if (!string.Equals(currency, "EUR", StringComparison.OrdinalIgnoreCase))
                eur = await fx.ToEurAsync(reading.Price, currency, asOf, ct);

            tickers.Add(new TickerContext(
                ticker, currency, reading.Price, eur, reading.Zone, reading.Reasons));
        }

        // Build the per-instrument price map for portfolio valuation. Only Held
        // instruments contribute to the portfolio (watchlist items don't have
        // positions). Lookup the EUR price from the per-ticker indicator loop.
        var priceMap = new Dictionary<int, (decimal PriceEur, string Ticker, string Currency)>();
        foreach (var inst in instruments.Where(i => i.Kind == InstrumentKind.Held))
        {
            var ctx = tickers.SingleOrDefault(t => t.Ticker == inst.Ticker);
            var priceEur = ctx?.PriceEur ?? ctx?.PriceNative ?? 0m;
            priceMap[inst.Id] = (priceEur, inst.Ticker, inst.Currency);
        }

        var snap = await portfolio.SnapshotAsync(asOf, priceMap, goal.TargetEur, ct);

        var asOfTrades = await tradeRepo.ListAsync(new TradesAsOfSpec(asOf), ct);
        var recentDtos = asOfTrades
            .OrderByDescending(t => t.ExecutedOn).Take(20)
            .OrderBy(t => t.ExecutedOn)
            .Select(t => new TradeRecent(t.ExecutedOn, t.Side, t.Quantity, t.PricePerShare))
            .ToList();

        decimal? usdPerEur = null;
        try
        {
            var oneUsdInEur = await fx.ToEurAsync(1m, "USD", asOf, ct);
            if (oneUsdInEur != 0m) usdPerEur = 1m / oneUsdInEur;
        }
        catch (Common.Exceptions.FxRateUnavailableException)
        {
            // Tolerant — snapshot can be built without the FX rate present.
        }

        var promptHash = HashPrompt(asOf, snap, tickers, recentDtos);

        return new AiSnapshot(asOf, goal, snap, tickers, recentDtos, usdPerEur, [], promptHash);
    }

    private static string HashPrompt(DateOnly today, PortfolioSnapshot snap,
        IEnumerable<TickerContext> tickers, IEnumerable<TradeRecent> recent)
    {
        var payload = new { today, snap, tickers, recent };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOpts.Strict));
        return Convert.ToHexString(SHA256.HashData(bytes))[..16];
    }
}
