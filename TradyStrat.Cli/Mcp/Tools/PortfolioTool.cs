using System.ComponentModel;
using Ardalis.Specification;
using ModelContextProtocol.Server;
using TradyStrat.Application.Fx;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.PriceFeed.Specifications;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Cli.Mcp.Mapping;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Cli.Mcp.Tools;

[McpServerToolType]
internal sealed class PortfolioTool(
    IPortfolioRepository portfolios,
    IReadRepositoryBase<Instrument> instruments,
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
        var instrumentById = allInstruments.ToDictionary(i => new InstrumentId(i.Id), i => i);
        var fx = new FxConverter(fxRates);

        var priceByInstrument = new Dictionary<InstrumentId, Price>();
        foreach (var inst in allInstruments.Where(i => i.Kind == InstrumentKind.Held))
        {
            var priceBars = await bars.ListAsync(new PriceBarsAsOfSpec(inst.Ticker, target), ct);
            if (priceBars.Count == 0) continue;

            var latestClose = priceBars[^1].Close;
            decimal priceEur = string.Equals(inst.Currency, "EUR", StringComparison.OrdinalIgnoreCase)
                ? latestClose
                : await fx.ToEurAsync(latestClose, inst.Currency, target, ct);

            priceByInstrument[new InstrumentId(inst.Id)] =
                Price.Of(Money.Of(priceEur, Currency.Eur));
        }

        var goal = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(DateTime.UtcNow);
        var goalEur = goal.TargetEur;
        var goalTarget = Money.Of(goalEur, Currency.Eur);

        var portfolio = await portfolios.GetAsync(ct);
        var domainSnapshot = portfolio.SnapshotAsOf(target, instrumentById, priceByInstrument, goalTarget);

        // Flatten trades with their originating InstrumentId for the mapper.
        var allTrades = portfolio.Positions
            .SelectMany(p => p.Trades.Select(t => (Trade: t, InstrumentId: p.InstrumentId.Value)))
            .OrderByDescending(x => x.Trade.ExecutedOn)
            .ToList();
        var tickerByInstrumentId = allInstruments.ToDictionary(i => i.Id, i => i.Ticker);

        return PortfolioMapper.ToSnapshot(domainSnapshot, allTrades, tickerByInstrumentId, goalEur, target);
    }
}
