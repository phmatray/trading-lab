namespace TradingStrat.Domain.Entities;

public record BacktestResult(
    string StrategyName,
    string StrategyDescription,
    Dictionary<string, object> StrategyParameters,
    string Ticker,
    DateTime StartDate,
    DateTime EndDate,
    decimal InitialCapital,
    decimal CommissionPercentage,
    decimal MinimumCommission,
    List<Trade> Trades,
    List<EquityPoint> EquityCurve,
    PerformanceMetrics Metrics
);
