using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.Time;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Features.PredictionMarkets;
using TradyStrat.Features.Settings.UseCases;
using TradyStrat.Features.Trades.Specifications;

namespace TradyStrat.Features.AiSuggestion.Snapshot;

public sealed class AiSnapshotService(
    IndicatorEngine indicators,
    PortfolioService portfolio,
    FxConverter fx,
    IReadRepositoryBase<GoalConfig> goalRepo,
    IReadRepositoryBase<Trade> tradeRepo,
    ListInstrumentsUseCase listInstruments,
    IPredictionMarketProvider predictionMarkets,
    ILogger<AiSnapshotService> log,
    IClock clock) : IAiSnapshotService
{
    // Preserve legacy iteration order [COIN, BTC-USD] so the day-one PromptHash
    // remains byte-identical against the pre-multi-ticker fixture. New watchlist
    // instruments fall through to alphabetical order via the ThenBy below.
    private static readonly string[] LegacyWatchlistOrder = ["COIN", "BTC-USD"];

    public async Task<AiSnapshot> CreateAsync(int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        var goal = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(clock.UtcNow());

        var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);
        var primary = instruments.SingleOrDefault(i => i.Id == instrumentId)
            ?? throw new InvalidOperationException(
                $"Instrument id {instrumentId} is not in the Instruments table.");

        // Catalog order: primary first, then watchlist in legacy order, then any
        // newer watchlist instruments alphabetically. Held instruments other than
        // the primary stay context-only — they appear in `Tickers` for indicator
        // analysis but the prompt's primary subject is the passed `instrumentId`.
        var watchlist = instruments
            .Where(i => i.Kind == InstrumentKind.Watchlist)
            .OrderBy(i => Array.IndexOf(LegacyWatchlistOrder, i.Ticker) is var idx && idx < 0
                ? int.MaxValue : idx)
            .ThenBy(i => i.Ticker);
        var catalog = new[] { (primary.Ticker, primary.Currency) }
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

        // Prediction markets — graceful degradation, never blocks the AI call.
        IReadOnlyList<PredictionMarket> markets;
        try
        {
            markets = await predictionMarkets.GetMarketsAsync(ct);
        }
        catch (PolymarketUnavailableException ex)
        {
            AiSnapshotServiceLog.PolymarketUnavailable(log, ex);
            markets = [];
        }
        if (markets.Count == 0)
        {
            AiSnapshotServiceLog.PolymarketEmpty(log);
        }

        var promptHash = HashPrompt(asOf, snap, tickers, recentDtos, markets);

        return new AiSnapshot(
            asOf,
            instrumentId,                              // NEW
            goal, snap, tickers, recentDtos,
            usdPerEur, markets, promptHash);
    }

    private static string HashPrompt(DateOnly today, PortfolioSnapshot snap,
        IEnumerable<TickerContext> tickers, IEnumerable<TradeRecent> recent,
        IEnumerable<PredictionMarket> markets)
    {
        // Do NOT add instrumentId to this payload — see spec §4.7. Adding it
        // would break the AiSnapshotServiceTests sentinel "895EED53A280A470" and
        // make CON3.L's day-zero hash drift, defeating the no-regression check
        // on prompt-input shape across the Phase 2 refactor.
        var payload = new { today, snap, tickers, recent, markets };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOpts.Strict));
        return Convert.ToHexString(SHA256.HashData(bytes))[..16];
    }
}

internal static partial class AiSnapshotServiceLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Polymarket unavailable, snapshot will omit markets")]
    public static partial void PolymarketUnavailable(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Polymarket filter returned 0 markets — adjust Tags / MinVolumeUsd / MaxHorizonDays")]
    public static partial void PolymarketEmpty(ILogger logger);
}
