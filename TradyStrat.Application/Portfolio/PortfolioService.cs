using Ardalis.Specification;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Application.Trades.Specifications;

namespace TradyStrat.Application.Portfolio;

public sealed class PortfolioService(IReadRepositoryBase<Trade> trades)
{
    public async Task<PortfolioSnapshot> SnapshotAsync(
        IReadOnlyDictionary<int, (decimal PriceEur, string Ticker, string Currency)> priceByInstrument,
        decimal goalEur,
        CancellationToken ct)
    {
        var all = await trades.ListAsync(new AllTradesSpec(), ct);
        return BuildSnapshot(all, priceByInstrument, goalEur);
    }

    public async Task<PortfolioSnapshot> SnapshotAsync(
        DateOnly asOf,
        IReadOnlyDictionary<int, (decimal PriceEur, string Ticker, string Currency)> priceByInstrument,
        decimal goalEur,
        CancellationToken ct)
    {
        var asOfTrades = await trades.ListAsync(new TradesAsOfSpec(asOf), ct);
        return BuildSnapshot(asOfTrades, priceByInstrument, goalEur);
    }

    private static PortfolioSnapshot BuildSnapshot(
        List<Trade> trades,
        IReadOnlyDictionary<int, (decimal PriceEur, string Ticker, string Currency)> priceByInstrument,
        decimal goalEur)
    {
        var positions = new List<PositionRow>();

        foreach (var group in trades.GroupBy(t => t.InstrumentId))
        {
            var openLots = new LinkedList<Lot>();
            var realized = 0m;

            foreach (var t in group.OrderBy(x => x.ExecutedOn))
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
                                $"Sell on {t.ExecutedOn} for instrument {group.Key} exceeds open lots.");

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

            var qty       = openLots.Sum(l => l.Quantity);
            var costBasis = openLots.Sum(l => l.CostBasisEur);

            // Graceful degradation: if an instrument has trades but is missing from the
            // price map (e.g. removed mid-flight — out of scope per Phase 1 spec),
            // market value defaults to 0 and ticker/currency to "?".
            var hasPrice = priceByInstrument.TryGetValue(group.Key, out var info);
            var marketValue = hasPrice ? qty * info.PriceEur : 0m;
            var ticker      = hasPrice ? info.Ticker   : "?";
            var currency    = hasPrice ? info.Currency : "?";
            var unrealised  = marketValue - costBasis;

            positions.Add(new PositionRow(
                InstrumentId: group.Key,
                Ticker: ticker,
                Currency: currency,
                Quantity: qty,
                CostBasisEur: costBasis,
                MarketValueEur: marketValue,
                UnrealizedPnLEur: unrealised,
                RealizedPnLEur: realized));
        }

        var totalValue  = positions.Sum(p => p.MarketValueEur);
        var totalCost   = positions.Sum(p => p.CostBasisEur);
        var totalUnreal = totalValue - totalCost;
        var totalReal   = positions.Sum(p => p.RealizedPnLEur);
        var pct         = goalEur == 0m ? 0m : totalValue / goalEur * 100m;

        // Legacy scalar fields (single-ticker callers like HeroCapital/PortfolioRail/
        // GrowthChart): populated only when there's exactly one position. Task 14 will
        // remove the dashboard reads of these.
        var legacyShares  = positions.Count == 1 ? positions[0].Quantity : 0m;
        var legacyAvgCost = positions.Count == 1 && legacyShares > 0
            ? positions[0].CostBasisEur / legacyShares
            : 0m;

        return new PortfolioSnapshot(
            Positions: positions,
            CurrentValueEur: totalValue,
            CostBasisEur: totalCost,
            UnrealizedPnLEur: totalUnreal,
            RealizedPnLEur: totalReal,
            ProgressPct: pct,
            Shares: legacyShares,
            AvgCostEur: legacyAvgCost);
    }
}
