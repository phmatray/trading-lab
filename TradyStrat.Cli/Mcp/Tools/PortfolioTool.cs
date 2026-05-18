using System.ComponentModel;
using Ardalis.Specification;
using ModelContextProtocol.Server;
using TradyStrat.Application.Fx;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.PriceFeed.Specifications;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Cli.Mcp.Mapping;
using TradyStrat.Domain;

namespace TradyStrat.Cli.Mcp.Tools;

[McpServerToolType]
internal sealed class PortfolioTool(
    PortfolioService portfolio,
    IReadRepositoryBase<Instrument> instruments,
    IReadRepositoryBase<Trade> trades,
    IReadRepositoryBase<PriceBar> bars,
    IReadRepositoryBase<FxRate> fxRates,
    IReadRepositoryBase<GoalConfig> goalRepo,
    IClock clock)
{
    [McpServerTool(Name = "get_portfolio"),
     Description("Current portfolio: per-ticker lots, aggregate value, progress toward goal.")]
    public async Task<PortfolioSnapshotDto> GetPortfolio(
        string? asOf = null,
        CancellationToken ct = default)
    {
        var (_, target) = Guards.ResolveDateRange(
            from: asOf, to: asOf, defaultBack: 0, clockToday: clock.TodayLocal());

        var allInstruments = await instruments.ListAsync(ct);
        var fx = new FxConverter(fxRates);

        // Build priceByInstrument from the latest PriceBar up to target for each held instrument.
        // Mirrors the logic in LoadDashboardUseCase but reads bars directly rather than going
        // through IIndicatorEngine (price only; no indicator metadata needed here).
        var priceByInstrument = new Dictionary<int, (decimal PriceEur, string Ticker, string Currency)>();
        foreach (var inst in allInstruments.Where(i => i.Kind == InstrumentKind.Held))
        {
            var priceBars = await bars.ListAsync(new PriceBarsAsOfSpec(inst.Ticker, target), ct);
            if (priceBars.Count == 0) continue;

            var latestClose = priceBars[^1].Close;
            decimal priceEur = string.Equals(inst.Currency, "EUR", StringComparison.OrdinalIgnoreCase)
                ? latestClose
                : await fx.ToEurAsync(latestClose, inst.Currency, target, ct);

            priceByInstrument[inst.Id] = (priceEur, inst.Ticker, inst.Currency);
        }

        var goal = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(DateTime.UtcNow);
        var goalEur = goal.TargetEur;

        var domainSnapshot = await portfolio.SnapshotAsync(target, priceByInstrument, goalEur, ct);

        var allTrades = await trades.ListAsync(ct);
        var tickerByInstrumentId = allInstruments.ToDictionary(i => i.Id, i => i.Ticker);

        return PortfolioMapper.ToSnapshot(domainSnapshot, allTrades, tickerByInstrumentId, goalEur, target);
    }
}
