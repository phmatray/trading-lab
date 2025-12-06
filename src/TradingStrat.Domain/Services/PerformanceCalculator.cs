using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.Services;

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

        decimal finalEquity = equityCurve[^1].Equity;
        decimal totalReturn = finalEquity - initialCapital;
        decimal totalReturnPercentage = (totalReturn / initialCapital) * 100;

        decimal daysInYears = totalDays / 252m;
        decimal annualizedReturn = daysInYears > 0
            ? (decimal)Math.Pow((double)(finalEquity / initialCapital), (double)(1 / daysInYears)) - 1
            : 0;
        annualizedReturn *= 100;

        var buyTrades = trades.Where(t => t.Type == TradeType.Buy).ToList();
        var sellTrades = trades.Where(t => t.Type == TradeType.Sell).ToList();

        int roundTripTrades = Math.Min(buyTrades.Count, sellTrades.Count);
        int winningTrades = sellTrades.Count(t => t.ProfitLoss > 0);
        int losingTrades = sellTrades.Count(t => t.ProfitLoss < 0);
        decimal winRate = sellTrades.Count > 0 ? (decimal)winningTrades / sellTrades.Count * 100 : 0;

        var wins = sellTrades.Where(t => t.ProfitLoss > 0).Select(t => t.ProfitLoss ?? 0).ToList();
        var losses = sellTrades.Where(t => t.ProfitLoss < 0).Select(t => Math.Abs(t.ProfitLoss ?? 0)).ToList();

        decimal averageWin = wins.Count > 0 ? wins.Average() : 0;
        decimal averageLoss = losses.Count > 0 ? losses.Average() : 0;
        decimal largestWin = wins.Count > 0 ? wins.Max() : 0;
        decimal largestLoss = losses.Count > 0 ? losses.Max() : 0;

        decimal grossProfit = wins.Sum();
        decimal grossLoss = losses.Sum();
        decimal profitFactor = grossLoss > 0 ? grossProfit / grossLoss : 0;

        (int maxConsecutiveWins, int maxConsecutiveLosses) = CalculateConsecutiveWinsLosses(sellTrades);

        (decimal maxDrawdown, decimal maxDrawdownPercentage) = CalculateMaxDrawdown(equityCurve);

        List<decimal> dailyReturns = CalculateDailyReturns(equityCurve);
        decimal sharpeRatio = CalculateSharpeRatio(dailyReturns);
        decimal volatility = CalculateVolatility(dailyReturns) * 100;

        int daysInMarket = equityCurve.Count(e => e.Position > 0);
        decimal marketExposurePercentage = totalDays > 0 ? (decimal)daysInMarket / totalDays * 100 : 0;

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
        {
            return (0, 0);
        }

        int currentWinStreak = 0;
        int currentLossStreak = 0;
        int maxWinStreak = 0;
        int maxLossStreak = 0;

        foreach (Trade trade in sellTrades)
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
        {
            return (0, 0);
        }

        decimal maxEquity = equityCurve[0].Equity;
        decimal maxDrawdown = 0;
        decimal maxDrawdownPercentage = 0;

        foreach (EquityPoint point in equityCurve)
        {
            if (point.Equity > maxEquity)
            {
                maxEquity = point.Equity;
            }

            decimal drawdown = maxEquity - point.Equity;
            decimal drawdownPercentage = maxEquity > 0 ? (drawdown / maxEquity) * 100 : 0;

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
            decimal previousEquity = equityCurve[i - 1].Equity;
            decimal currentEquity = equityCurve[i].Equity;

            if (previousEquity > 0)
            {
                decimal dailyReturn = (currentEquity - previousEquity) / previousEquity;
                returns.Add(dailyReturn);
            }
        }

        return returns;
    }

    private decimal CalculateSharpeRatio(List<decimal> dailyReturns)
    {
        if (dailyReturns.Count < 2)
        {
            return 0;
        }

        decimal averageReturn = dailyReturns.Average();
        decimal stdDev = CalculateStandardDeviation(dailyReturns);

        if (stdDev == 0)
        {
            return 0;
        }

        decimal sharpeRatio = averageReturn / stdDev;
        return sharpeRatio * (decimal)Math.Sqrt(252);
    }

    private decimal CalculateVolatility(List<decimal> dailyReturns)
    {
        if (dailyReturns.Count < 2)
        {
            return 0;
        }

        decimal stdDev = CalculateStandardDeviation(dailyReturns);
        return stdDev * (decimal)Math.Sqrt(252);
    }

    private decimal CalculateStandardDeviation(List<decimal> values)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        decimal average = values.Average();
        double sumOfSquares = values.Sum(v => Math.Pow((double)(v - average), 2));
        double variance = sumOfSquares / (values.Count - 1);
        return (decimal)Math.Sqrt(variance);
    }
}
