namespace TradyStrat.Cli.Mcp.Dto;

public sealed record PortfolioSnapshotDto(
    DateOnly AsOfDate, decimal FxRate,
    AggregateBlock Aggregate,
    IReadOnlyList<PositionRow> Positions,
    IReadOnlyList<TradeRow> Trades,
    bool TradesTruncated);

public sealed record AggregateBlock(
    decimal TotalValueEur, decimal GoalEur,
    decimal DistanceToGoalEur, decimal ProgressPct);

public sealed record PositionRow(
    string Ticker, int Qty, decimal AvgCostEur,
    decimal MarketValueUsd, decimal MarketValueEur,
    decimal RealizedPnlUsd, decimal UnrealizedPnlUsd,
    decimal RealizedPnlEur, decimal UnrealizedPnlEur);

public sealed record TradeRow(DateOnly Date, string Ticker, string Side, int Qty, decimal PriceUsd);
