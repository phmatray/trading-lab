using Ardalis.Specification;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Specifications.Trades;

namespace TradyStrat.Features.Portfolio;

public sealed class PortfolioService(IReadRepositoryBase<Trade> trades)
{
    public async Task<PortfolioSnapshot> SnapshotAsync(
        decimal currentPriceEur, decimal goalEur, CancellationToken ct)
    {
        var all = await trades.ListAsync(new AllTradesSpec(), ct);

        var openLots = new LinkedList<Lot>();
        var realized = 0m;

        foreach (var t in all)
        {
            if (t.IsBuy)
            {
                var unitCost = (t.GrossEur + t.FeesEur) / t.Quantity;   // fees folded into cost basis
                openLots.AddLast(new Lot(t.ExecutedOn, t.Quantity, unitCost));
            }
            else
            {
                var remaining = t.Quantity;
                while (remaining > 0)
                {
                    var head = openLots.First
                        ?? throw new TradeValidationException(
                            $"Sell on {t.ExecutedOn} exceeds open lots.");

                    var consumed = Math.Min(head.Value.Quantity, remaining);
                    realized += consumed * (t.PricePerShare - head.Value.UnitCostEur);
                    realized -= t.FeesEur * (consumed / t.Quantity);

                    if (consumed == head.Value.Quantity)
                        openLots.RemoveFirst();
                    else
                        head.Value = head.Value with { Quantity = head.Value.Quantity - consumed };

                    remaining -= consumed;
                }
            }
        }

        var shares     = openLots.Sum(l => l.Quantity);
        var costBasis  = openLots.Sum(l => l.CostBasisEur);
        var avgCost    = shares == 0 ? 0m : costBasis / shares;
        var currentVal = shares * currentPriceEur;
        var unrealised = currentVal - costBasis;
        var pct        = goalEur == 0m ? 0m : currentVal / goalEur * 100m;

        return new PortfolioSnapshot(
            Shares: shares,
            AvgCostEur: avgCost,
            CurrentValueEur: currentVal,
            UnrealizedPnLEur: unrealised,
            RealizedPnLEur: realized,
            ProgressPct: pct);
    }
}
