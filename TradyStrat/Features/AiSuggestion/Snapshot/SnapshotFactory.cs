using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ardalis.Specification;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Time;
using TradyStrat.Features.Trades.Specifications;

namespace TradyStrat.Features.AiSuggestion.Snapshot;

public sealed class SnapshotFactory(
    IndicatorEngine indicators,
    PortfolioService portfolio,
    FxConverter fx,
    IReadRepositoryBase<GoalConfig> goalRepo,
    IReadRepositoryBase<Trade> tradeRepo,
    IClock clock) : ISnapshotFactory
{
    private const string FocusTicker = "CON3.L";

    private static readonly (string Ticker, string Currency)[] Catalog =
    [
        (FocusTicker, "USD"),
        ("COIN",      "USD"),
        ("BTC-USD",   "USD"),
    ];

    public async Task<AiSnapshot> CreateAsync(DateOnly asOf, CancellationToken ct)
    {
        var goal = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(clock.UtcNow());

        var tickers = new List<TickerContext>();
        decimal? focusPriceEur = null;

        foreach (var (ticker, currency) in Catalog)
        {
            var reading = await indicators.ComputeFor(ticker, asOf, ct);
            decimal? eur = null;
            if (currency == "USD")
                eur = await fx.ToEurAsync(reading.Price, currency, asOf, ct);

            // Portfolio math is in EUR, so use the EUR-converted focus price.
            if (ticker == FocusTicker) focusPriceEur = eur ?? reading.Price;

            tickers.Add(new TickerContext(
                ticker, currency, reading.Price, eur, reading.Zone, reading.Reasons));
        }

        var snap = await portfolio.SnapshotAsync(asOf, focusPriceEur ?? 0m, goal.TargetEur, ct);

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

        return new AiSnapshot(asOf, goal, snap, tickers, recentDtos, usdPerEur, promptHash);
    }

    private static string HashPrompt(DateOnly today, PortfolioSnapshot snap,
        IEnumerable<TickerContext> tickers, IEnumerable<TradeRecent> recent)
    {
        var payload = new { today, snap, tickers, recent };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOpts.Strict));
        return Convert.ToHexString(SHA256.HashData(bytes))[..16];
    }
}
