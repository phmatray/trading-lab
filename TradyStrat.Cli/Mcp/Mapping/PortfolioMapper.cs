using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Domain;
using DomainPortfolio = TradyStrat.Domain.PortfolioSnapshot;

namespace TradyStrat.Cli.Mcp.Mapping;

internal static class PortfolioMapper
{
    private const int TradeCap = 500;

    public static PortfolioSnapshotDto ToSnapshot(
        DomainPortfolio src,
        IReadOnlyList<Trade> ledger,
        IReadOnlyDictionary<int, string> tickerByInstrumentId,
        decimal goalEur,
        DateOnly asOf)
    {
        var ordered = ledger.OrderByDescending(t => t.ExecutedOn).ToList();
        var truncated = ordered.Count > TradeCap;
        var trades = (truncated ? ordered.Take(TradeCap) : ordered)
            .Select(t => new TradeRow(
                Date: t.ExecutedOn,
                Ticker: tickerByInstrumentId.TryGetValue(t.InstrumentId, out var tk) ? tk : "(unknown)",
                Side: t.Side.ToString(),
                Qty: t.Quantity,
                PricePerShareEur: t.PricePerShare,
                FeesEur: t.FeesEur))
            .ToList();

        var positions = src.Positions
            .Select(p => new PositionRowDto(
                Ticker: p.Ticker,
                Currency: p.Currency,
                Qty: p.Quantity,
                CostBasisEur: p.CostBasisEur,
                MarketValueEur: p.MarketValueEur,
                UnrealizedPnlEur: p.UnrealizedPnLEur,
                RealizedPnlEur: p.RealizedPnLEur))
            .ToList();

        return new PortfolioSnapshotDto(
            AsOfDate: asOf,
            Aggregate: new AggregateBlock(
                TotalValueEur: src.CurrentValueEur,
                CostBasisEur: src.CostBasisEur,
                UnrealizedPnlEur: src.UnrealizedPnLEur,
                RealizedPnlEur: src.RealizedPnLEur,
                GoalEur: goalEur,
                DistanceToGoalEur: goalEur - src.CurrentValueEur,
                ProgressPct: src.ProgressPct),
            Positions: positions,
            Trades: trades,
            TradesTruncated: truncated);
    }
}
