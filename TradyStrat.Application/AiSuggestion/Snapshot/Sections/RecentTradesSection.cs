using TradyStrat.Application.Portfolio;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;

namespace TradyStrat.Application.AiSuggestion.Snapshot.Sections;

public sealed class RecentTradesSection(
    IPortfolioRepository portfolios) : ISnapshotSectionProvider
{
    public int Order => 40;

    public async Task ContributeAsync(SnapshotBuilder builder, int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        var portfolio = await portfolios.GetAsync(ct);
        var asOfTrades = portfolio.Positions
            .SelectMany(p => p.Trades)
            .Where(t => t.ExecutedOn <= asOf);

        var recent = asOfTrades
            .OrderByDescending(t => t.ExecutedOn).Take(20)
            .OrderBy(t => t.ExecutedOn)
            .Select(t => new TradeRecent(
                t.ExecutedOn, t.Side,
                t.Quantity.Value,
                t.PricePerShare.PerUnit.Amount));

        builder.RecentTrades.AddRange(recent);
    }
}
