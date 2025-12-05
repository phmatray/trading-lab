namespace TradingStrat.Models;

public record PerformanceMetrics(
    decimal InitialCapital,
    decimal FinalEquity,
    decimal TotalReturn,
    decimal TotalReturnPercentage,
    decimal AnnualizedReturn,
    int TotalTrades,
    int WinningTrades,
    int LosingTrades,
    decimal WinRate,
    decimal AverageWin,
    decimal AverageLoss,
    decimal LargestWin,
    decimal LargestLoss,
    decimal ProfitFactor,
    int MaxConsecutiveWins,
    int MaxConsecutiveLosses,
    decimal MaxDrawdown,
    decimal MaxDrawdownPercentage,
    decimal SharpeRatio,
    decimal Volatility,
    int TotalDays,
    int DaysInMarket,
    decimal MarketExposurePercentage
)
{
    public int NewRecords { get; init; }
}
