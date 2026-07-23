using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using DomainPortfolio = TradyStrat.Domain.Portfolio.PortfolioSnapshot;

namespace TradyStrat.Cli.Mcp.Mapping;

internal static class PortfolioMapper
{
    private const int TradeCap = 500;

    public static PortfolioSnapshotDto ToSnapshot(
        DomainPortfolio src,
        IReadOnlyList<(Trade Trade, int InstrumentId)> ledger,
        IReadOnlyDictionary<int, string> tickerByInstrumentId,
        decimal goalEur,
        DateOnly asOf)
    {
        var ordered = ledger.OrderByDescending(x => x.Trade.ExecutedOn).ToList();
        var truncated = ordered.Count > TradeCap;
        var trades = (truncated ? ordered.Take(TradeCap) : ordered)
            .Select(x => new TradeRow(
                Date: x.Trade.ExecutedOn,
                Ticker: tickerByInstrumentId.TryGetValue(x.InstrumentId, out var tk) ? tk : "(unknown)",
                Side: x.Trade.Side.ToString(),
                Qty: x.Trade.Quantity.Value,
                PricePerShareEur: x.Trade.PricePerShare.PerUnit.Amount,
                FeesEur: x.Trade.Fees.Amount))
            .ToList();

        var positions = src.Positions
            .Select(p => new PositionRowDto(
                Ticker: p.Ticker,
                Currency: p.Currency,
                Qty: p.Quantity.Value,
                CostBasisEur: p.CostBasisEur.Amount,
                MarketValueEur: p.MarketValueEur.Amount,
                UnrealizedPnlEur: p.UnrealizedPnLEur.Amount,
                RealizedPnlEur: p.RealizedPnLEur.Amount))
            .ToList();

        return new PortfolioSnapshotDto(
            AsOfDate: asOf,
            Aggregate: new AggregateBlock(
                TotalValueEur: src.CurrentValueEur.Amount,
                CostBasisEur: src.CostBasisEur.Amount,
                UnrealizedPnlEur: src.UnrealizedPnLEur.Amount,
                RealizedPnlEur: src.RealizedPnLEur.Amount,
                GoalEur: goalEur,
                DistanceToGoalEur: goalEur - src.CurrentValueEur.Amount,
                ProgressPct: src.ProgressPct),
            Positions: positions,
            Trades: trades,
            TradesTruncated: truncated);
    }
}
