namespace TradyStrat.Cli.Mcp.Dto;

public sealed record PortfolioSnapshotDto(
    DateOnly AsOfDate,
    AggregateBlock Aggregate,
    IReadOnlyList<PositionRowDto> Positions,
    IReadOnlyList<TradeRow> Trades,
    bool TradesTruncated);

public sealed record AggregateBlock(
    decimal TotalValueEur,
    decimal CostBasisEur,
    decimal UnrealizedPnlEur,
    decimal RealizedPnlEur,
    decimal GoalEur,
    decimal DistanceToGoalEur,
    decimal ProgressPct);

public sealed record PositionRowDto(
    string Ticker,
    string Currency,
    decimal Qty,
    decimal CostBasisEur,
    decimal MarketValueEur,
    decimal UnrealizedPnlEur,
    decimal RealizedPnlEur);

public sealed record TradeRow(
    DateOnly Date,
    string Ticker,
    string Side,
    decimal Qty,
    decimal PricePerShareEur,
    decimal FeesEur);
