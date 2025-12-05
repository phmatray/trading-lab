namespace TradingStrat.Services.Backtesting;

public enum PositionSizingMode
{
    AllIn,
    Fixed,
    Percentage
}

public record BacktestConfiguration(
    string Ticker,
    DateTime StartDate,
    DateTime EndDate,
    decimal InitialCapital,
    decimal CommissionPercentage,
    decimal MinimumCommission,
    PositionSizingMode PositionSizing = PositionSizingMode.AllIn,
    int? FixedQuantity = null,
    decimal? PositionPercentage = null
);
