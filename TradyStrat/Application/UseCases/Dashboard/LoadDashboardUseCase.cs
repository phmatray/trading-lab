using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using TradyStrat.Application.Abstractions;
using TradyStrat.Application.UseCases.AiSuggestion;
using TradyStrat.Features.Dashboard;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Time;
using TradyStrat.Specifications.PriceBars;
using TradyStrat.Specifications.Trades;

namespace TradyStrat.Application.UseCases.Dashboard;

public sealed class LoadDashboardUseCase(
    IndicatorEngine indicators,
    PortfolioService portfolio,
    GrowthSeriesBuilder growth,
    FxConverter fx,
    IReadRepositoryBase<GoalConfig> goalRepo,
    IReadRepositoryBase<Trade> tradeRepo,
    IReadRepositoryBase<PriceBar> priceRepo,
    GetTodaysSuggestionUseCase todaysSuggestion,
    IClock clock,
    ILogger<LoadDashboardUseCase> log)
    : UseCaseBase<Unit, DashboardViewModel>(log)
{
    private const string FocusTicker = "CON3.L";

    private static readonly (string Ticker, string Currency)[] Catalog =
    [
        (FocusTicker, "USD"),
        ("COIN",      "USD"),
        ("BTC-USD",   "USD"),
    ];

    protected override async Task<DashboardViewModel> ExecuteCore(Unit _, CancellationToken ct)
    {
        var today = clock.TodayInExchangeTzFor(FocusTicker);
        var goal  = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(clock.UtcNow());

        var tickers = new List<TickerView>();
        decimal? focusPriceEur = null;

        foreach (var (ticker, currency) in Catalog)
        {
            var reading = await indicators.ComputeFor(ticker, ct);
            decimal? eur = null;
            if (currency == "USD")
                eur = await fx.UsdToEurAsync(reading.Price, today, ct);
            if (ticker == FocusTicker) focusPriceEur = eur ?? reading.Price;

            var deltaPct = await ComputeDeltaPctAsync(ticker, ct);

            tickers.Add(new TickerView(
                ticker, currency, reading.Price, eur, deltaPct, reading.Zone));
        }

        var snap = await portfolio.SnapshotAsync(focusPriceEur ?? 0m, goal.TargetEur, ct);
        var growthSeries = await growth.BuildAsync(FocusTicker, ct);
        var todays = await todaysSuggestion.ExecuteAsync(Unit.Value, ct);
        var entryNum = await tradeRepo.CountAsync(new AllTradesSpec(), ct);
        var latestBar = await priceRepo.FirstOrDefaultAsync(
            new LatestPriceBarSpec(FocusTicker), ct);

        return new DashboardViewModel(
            Today: today,
            EntryNumber: entryNum,
            Portfolio: snap,
            Goal: goal,
            TodaysCall: todays,
            Tickers: tickers,
            Growth: growthSeries,
            LatestPriceDate: latestBar?.Date);
    }

    private async Task<decimal?> ComputeDeltaPctAsync(string ticker, CancellationToken ct)
    {
        var bars = await priceRepo.ListAsync(new PriceBarsForTickerSpec(ticker), ct);
        if (bars.Count < 2) return null;
        var prev = bars[^2].Close;
        var curr = bars[^1].Close;
        if (prev == 0m) return null;
        return (curr - prev) / prev * 100m;
    }
}
