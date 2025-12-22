namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Domain value object for backtest configuration (zero external dependencies).
/// </summary>
public sealed record BacktestConfig(
    string Ticker,
    DateTime StartDate,
    DateTime EndDate,
    decimal InitialCapital,
    decimal CommissionPercentage,
    decimal MinimumCommission
);
