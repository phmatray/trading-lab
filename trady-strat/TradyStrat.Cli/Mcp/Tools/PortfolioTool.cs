using System.ComponentModel;
using ModelContextProtocol.Server;
using TradyStrat.Application.Fx;
using TradyStrat.Application.Goals;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Settings;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Cli.Mcp.Mapping;
using TradyStrat.Domain;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.PriceFeed;
using TradyStrat.Domain.Shared.Money;
using TradyStrat.Domain.Shared.Market;

namespace TradyStrat.Cli.Mcp.Tools;

[McpServerToolType]
internal sealed class PortfolioTool(
    IPortfolioRepository portfolios,
    IInstrumentRepository instruments,
    IPriceBarReadRepository bars,
    IFxRateReadRepository fxRates,
    IGoalRepository goalRepo,
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
        var instrumentById = allInstruments.ToDictionary(i => i.Id, i => i);
        var fx = new FxConverter(fxRates);

        var priceByInstrument = new Dictionary<InstrumentId, Price>();
        foreach (var inst in allInstruments.Where(i => i.Kind == InstrumentKind.Held))
        {
            var priceBars = await bars.ListAsOfAsync(inst.Ticker, target, ct);
            if (priceBars.Count == 0) continue;

            var latestClose = priceBars[^1].Close;
            decimal priceEur = inst.Currency == Currency.Eur
                ? latestClose
                : await fx.ToEurAsync(latestClose, inst.Currency.Code, target, ct);

            priceByInstrument[inst.Id] =
                Price.Of(Money.Of(priceEur, Currency.Eur));
        }

        var goal = await goalRepo.GetAsync(ct);
        var goalTarget = goal.Target;
        var goalEur = goalTarget.Amount;

        var portfolio = await portfolios.GetAsync(ct);
        var domainSnapshot = portfolio.SnapshotAsOf(target, instrumentById, priceByInstrument, goalTarget);

        // Flatten trades with their originating InstrumentId for the mapper.
        var allTrades = portfolio.Positions
            .SelectMany(p => p.Trades.Select(t => (Trade: t, InstrumentId: p.InstrumentId.Value)))
            .OrderByDescending(x => x.Trade.ExecutedOn)
            .ToList();
        var tickerByInstrumentId = allInstruments.ToDictionary(i => i.Id.Value, i => i.Ticker);

        return PortfolioMapper.ToSnapshot(domainSnapshot, allTrades, tickerByInstrumentId, goalEur, target);
    }
}
