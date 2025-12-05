using TradingStrat.Models;

namespace TradingStrat.Services.Backtesting;

public class PerformanceCalculator
{
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

        var finalEquity = equityCurve[^1].Equity;
        var totalReturn = finalEquity - initialCapital;
        var totalReturnPercentage = (totalReturn / initialCapital) * 100;

        var daysInYears = totalDays / 252m;
        var annualizedReturn = daysInYears > 0
            ? (decimal)Math.Pow((double)(finalEquity / initialCapital), (double)(1 / daysInYears)) - 1
            : 0;
        annualizedReturn *= 100;

        var buyTrades = trades.Where(t => t.Type == TradeType.Buy).ToList();
        var sellTrades = trades.Where(t => t.Type == TradeType.Sell).ToList();

        var roundTripTrades = Math.Min(buyTrades.Count, sellTrades.Count);
        var winningTrades = sellTrades.Count(t => t.ProfitLoss > 0);
        var losingTrades = sellTrades.Count(t => t.ProfitLoss < 0);
        var winRate = sellTrades.Count > 0 ? (decimal)winningTrades / sellTrades.Count * 100 : 0;

        var wins = sellTrades.Where(t => t.ProfitLoss > 0).Select(t => t.ProfitLoss ?? 0).ToList();
        var losses = sellTrades.Where(t => t.ProfitLoss < 0).Select(t => Math.Abs(t.ProfitLoss ?? 0)).ToList();

        var averageWin = wins.Count > 0 ? wins.Average() : 0;
        var averageLoss = losses.Count > 0 ? losses.Average() : 0;
        var largestWin = wins.Count > 0 ? wins.Max() : 0;
        var largestLoss = losses.Count > 0 ? losses.Max() : 0;

        var grossProfit = wins.Sum();
        var grossLoss = losses.Sum();
        var profitFactor = grossLoss > 0 ? grossProfit / grossLoss : 0;

        var (maxConsecutiveWins, maxConsecutiveLosses) = CalculateConsecutiveWinsLosses(sellTrades);

        var (maxDrawdown, maxDrawdownPercentage) = CalculateMaxDrawdown(equityCurve);

        var dailyReturns = CalculateDailyReturns(equityCurve);
        var sharpeRatio = CalculateSharpeRatio(dailyReturns);
        var volatility = CalculateVolatility(dailyReturns) * 100;

        var daysInMarket = equityCurve.Count(e => e.Position > 0);
        var marketExposurePercentage = totalDays > 0 ? (decimal)daysInMarket / totalDays * 100 : 0;

        return new PerformanceMetrics(
            InitialCapital: initialCapital,
            FinalEquity: finalEquity,
            TotalReturn: totalReturn,
            TotalReturnPercentage: totalReturnPercentage,
            AnnualizedReturn: annualizedReturn,
            TotalTrades: roundTripTrades,
            WinningTrades: winningTrades,
            LosingTrades: losingTrades,
            WinRate: winRate,
            AverageWin: averageWin,
            AverageLoss: averageLoss,
            LargestWin: largestWin,
            LargestLoss: largestLoss,
            ProfitFactor: profitFactor,
            MaxConsecutiveWins: maxConsecutiveWins,
            MaxConsecutiveLosses: maxConsecutiveLosses,
            MaxDrawdown: maxDrawdown,
            MaxDrawdownPercentage: maxDrawdownPercentage,
            SharpeRatio: sharpeRatio,
            Volatility: volatility,
            TotalDays: totalDays,
            DaysInMarket: daysInMarket,
            MarketExposurePercentage: marketExposurePercentage
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

    private (int maxWins, int maxLosses) CalculateConsecutiveWinsLosses(List<Trade> sellTrades)
    {
        if (sellTrades.Count == 0)
            return (0, 0);

        int currentWinStreak = 0;
        int currentLossStreak = 0;
        int maxWinStreak = 0;
        int maxLossStreak = 0;

        foreach (var trade in sellTrades)
        {
            if (trade.ProfitLoss > 0)
            {
                currentWinStreak++;
                currentLossStreak = 0;
                maxWinStreak = Math.Max(maxWinStreak, currentWinStreak);
            }
            else if (trade.ProfitLoss < 0)
            {
                currentLossStreak++;
                currentWinStreak = 0;
                maxLossStreak = Math.Max(maxLossStreak, currentLossStreak);
            }
        }

        return (maxWinStreak, maxLossStreak);
    }

    private (decimal maxDrawdown, decimal maxDrawdownPercentage) CalculateMaxDrawdown(List<EquityPoint> equityCurve)
    {
        if (equityCurve.Count == 0)
            return (0, 0);

        decimal maxEquity = equityCurve[0].Equity;
        decimal maxDrawdown = 0;
        decimal maxDrawdownPercentage = 0;

        foreach (var point in equityCurve)
        {
            if (point.Equity > maxEquity)
            {
                maxEquity = point.Equity;
            }

            var drawdown = maxEquity - point.Equity;
            var drawdownPercentage = maxEquity > 0 ? (drawdown / maxEquity) * 100 : 0;

            if (drawdown > maxDrawdown)
            {
                maxDrawdown = drawdown;
                maxDrawdownPercentage = drawdownPercentage;
            }
        }

        return (maxDrawdown, maxDrawdownPercentage);
    }

    private List<decimal> CalculateDailyReturns(List<EquityPoint> equityCurve)
    {
        var returns = new List<decimal>();

        for (int i = 1; i < equityCurve.Count; i++)
        {
            var previousEquity = equityCurve[i - 1].Equity;
            var currentEquity = equityCurve[i].Equity;

            if (previousEquity > 0)
            {
                var dailyReturn = (currentEquity - previousEquity) / previousEquity;
                returns.Add(dailyReturn);
            }
        }

        return returns;
    }

    private decimal CalculateSharpeRatio(List<decimal> dailyReturns)
    {
        if (dailyReturns.Count < 2)
            return 0;

        var averageReturn = dailyReturns.Average();
        var stdDev = CalculateStandardDeviation(dailyReturns);

        if (stdDev == 0)
            return 0;

        var sharpeRatio = averageReturn / stdDev;
        return sharpeRatio * (decimal)Math.Sqrt(252);
    }

    private decimal CalculateVolatility(List<decimal> dailyReturns)
    {
        if (dailyReturns.Count < 2)
            return 0;

        var stdDev = CalculateStandardDeviation(dailyReturns);
        return stdDev * (decimal)Math.Sqrt(252);
    }

    private decimal CalculateStandardDeviation(List<decimal> values)
    {
        if (values.Count < 2)
            return 0;

        var average = values.Average();
        var sumOfSquares = values.Sum(v => Math.Pow((double)(v - average), 2));
        var variance = sumOfSquares / (values.Count - 1);
        return (decimal)Math.Sqrt(variance);
    }
}
