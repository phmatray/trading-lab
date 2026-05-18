using Ardalis.Specification;
using TradyStrat.Application.Trades.Specifications;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.Snapshot.Sections;

public sealed class RecentTradesSection(
    IReadRepositoryBase<Trade> tradeRepo) : ISnapshotSectionProvider
{
    public int Order => 40;

    public async Task ContributeAsync(SnapshotBuilder builder, int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        var asOfTrades = await tradeRepo.ListAsync(new TradesAsOfSpec(asOf), ct);
        var recent = asOfTrades
            .OrderByDescending(t => t.ExecutedOn).Take(20)
            .OrderBy(t => t.ExecutedOn)
            .Select(t => new TradeRecent(t.ExecutedOn, t.Side, t.Quantity, t.PricePerShare));

        builder.RecentTrades.AddRange(recent);
    }
}
