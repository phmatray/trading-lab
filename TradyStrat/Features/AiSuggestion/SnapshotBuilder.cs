using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ardalis.Specification;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Time;
using TradyStrat.Specifications.Trades;

namespace TradyStrat.Features.AiSuggestion;

public sealed class SnapshotBuilder(
    IndicatorEngine indicators,
    PortfolioService portfolio,
    FxConverter fx,
    IReadRepositoryBase<GoalConfig> goalRepo,
    IReadRepositoryBase<Trade> tradeRepo,
    IClock clock) : ISnapshotBuilder
{
    private static readonly (string Ticker, string Currency)[] Catalog =
    [
        ("CON3.DE", "EUR"),
        ("COIN",    "USD"),
        ("BTC-USD", "USD"),
    ];

    public async Task<AiSnapshot> BuildAsync(CancellationToken ct)
    {
        var today = clock.TodayInExchangeTzFor("CON3.DE");
        var goal  = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(clock.UtcNow());

        var tickers = new List<TickerContext>();
        decimal? con3Price = null;

        foreach (var (ticker, currency) in Catalog)
        {
            var reading = await indicators.ComputeFor(ticker, ct);
            decimal? eur = null;
            if (currency == "USD")
                eur = await fx.UsdToEurAsync(reading.Price, today, ct);

            if (ticker == "CON3.DE") con3Price = reading.Price;

            tickers.Add(new TickerContext(
                ticker, currency, reading.Price, eur, reading.Zone, reading.Reasons));
        }

        var snap = await portfolio.SnapshotAsync(
            currentPriceEur: con3Price ?? 0m,
            goalEur: goal.TargetEur,
            ct: ct);

        var recent = await tradeRepo.ListAsync(new LatestTradesSpec(20), ct);
        var recentDtos = recent.Select(t => new TradeRecent(
            t.ExecutedOn, t.Side, t.Quantity, t.PricePerShare)).ToList();

        decimal? usdPerEur = null;
        try
        {
            var oneEurInEur = await fx.UsdToEurAsync(1m, today, ct); // 1 / UsdPerEur
            if (oneEurInEur != 0m) usdPerEur = 1m / oneEurInEur;
        }
        catch (TradyStrat.Shared.Exceptions.FxRateUnavailableException)
        {
            // Tolerant — snapshot can be built without the FX rate present.
        }

        var promptHash = HashPrompt(today, snap, tickers, recentDtos);

        return new AiSnapshot(today, goal, snap, tickers, recentDtos, usdPerEur, promptHash);
    }

    private static string HashPrompt(DateOnly today, PortfolioSnapshot snap,
        IEnumerable<TickerContext> tickers, IEnumerable<TradeRecent> recent)
    {
        var payload = new { today, snap, tickers, recent };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOpts.Strict));
        return Convert.ToHexString(SHA256.HashData(bytes))[..16];
    }
}
