using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.Services;

/// <summary>
/// Orchestrates performance metric calculation by delegating to specialized services.
/// Coordinates TradeAnalyzer, EquityCurveAnalyzer, and RiskCalculator.
/// </summary>
public class PerformanceCalculator
{
    private readonly TradeAnalyzer _tradeAnalyzer;
    private readonly EquityCurveAnalyzer _equityCurveAnalyzer;
    private readonly RiskCalculator _riskCalculator;

    public PerformanceCalculator()
    {
        _tradeAnalyzer = new TradeAnalyzer();
        _equityCurveAnalyzer = new EquityCurveAnalyzer();
        _riskCalculator = new RiskCalculator();
    }

    public PerformanceMetrics Calculate(
        List<Trade> trades,
        List<EquityPoint> equityCurve,
        decimal initialCapital,
        int totalDays)
    {
        if (equityCurve.Count == 0)
        {
            return CreateEmptyMetrics(initialCapital);
        }

        // Calculate return metrics
        decimal finalEquity = equityCurve[^1].Equity;
        decimal totalReturn = finalEquity - initialCapital;
        decimal totalReturnPercentage = (totalReturn / initialCapital) * 100;

        decimal daysInYears = totalDays / 252m;
        decimal annualizedReturn = daysInYears > 0
            ? (decimal)Math.Pow((double)(finalEquity / initialCapital), (double)(1 / daysInYears)) - 1
            : 0;
        annualizedReturn *= 100;

        // Delegate to TradeAnalyzer
        TradeStatistics tradeStats = _tradeAnalyzer.Analyze(trades);

        // Delegate to EquityCurveAnalyzer
        EquityCurveStatistics equityStats = _equityCurveAnalyzer.Analyze(equityCurve, totalDays);
        List<decimal> dailyReturns = _equityCurveAnalyzer.CalculateDailyReturns(equityCurve);

        // Delegate to RiskCalculator
        RiskMetrics riskMetrics = _riskCalculator.Analyze(dailyReturns);

        return new PerformanceMetrics(
            InitialCapital: initialCapital,
            FinalEquity: finalEquity,
            TotalReturn: totalReturn,
            TotalReturnPercentage: totalReturnPercentage,
            AnnualizedReturn: annualizedReturn,
            TotalTrades: tradeStats.TotalTrades,
            WinningTrades: tradeStats.WinningTrades,
            LosingTrades: tradeStats.LosingTrades,
            WinRate: tradeStats.WinRate,
            AverageWin: tradeStats.AverageWin,
            AverageLoss: tradeStats.AverageLoss,
            LargestWin: tradeStats.LargestWin,
            LargestLoss: tradeStats.LargestLoss,
            ProfitFactor: tradeStats.ProfitFactor,
            MaxConsecutiveWins: tradeStats.MaxConsecutiveWins,
            MaxConsecutiveLosses: tradeStats.MaxConsecutiveLosses,
            MaxDrawdown: equityStats.MaxDrawdown,
            MaxDrawdownPercentage: equityStats.MaxDrawdownPercentage,
            SharpeRatio: riskMetrics.SharpeRatio,
            Volatility: riskMetrics.Volatility,
            TotalDays: totalDays,
            DaysInMarket: equityStats.DaysInMarket,
            MarketExposurePercentage: equityStats.MarketExposurePercentage
        );
    }

    private PerformanceMetrics CreateEmptyMetrics(decimal initialCapital)
    {
        return new PerformanceMetrics(
            InitialCapital: initialCapital,
            FinalEquity: initialCapital,
            TotalReturn: 0,
            TotalReturnPercentage: 0,
            AnnualizedReturn: 0,
            TotalTrades: 0,
            WinningTrades: 0,
            LosingTrades: 0,
            WinRate: 0,
            AverageWin: 0,
            AverageLoss: 0,
            LargestWin: 0,
            LargestLoss: 0,
            ProfitFactor: 0,
            MaxConsecutiveWins: 0,
            MaxConsecutiveLosses: 0,
            MaxDrawdown: 0,
            MaxDrawdownPercentage: 0,
            SharpeRatio: 0,
            Volatility: 0,
            TotalDays: 0,
            DaysInMarket: 0,
            MarketExposurePercentage: 0
        );
    }
}
