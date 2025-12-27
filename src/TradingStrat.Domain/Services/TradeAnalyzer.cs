using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.Services;

/// <summary>
/// Domain service focused on analyzing trade-specific metrics.
/// Extracts trade statistics like win rate, profit factor, and consecutive wins/losses.
/// </summary>
public class TradeAnalyzer
{
    /// <summary>
    /// Analyzes a collection of trades and returns comprehensive trade statistics.
    /// </summary>
    /// <param name="trades">List of all trades (both buy and sell).</param>
    /// <returns>Trade statistics including win rate, profit factor, and streaks.</returns>
    public TradeStatistics Analyze(List<Trade> trades)
    {
        List<Trade> buyTrades = trades.Where(t => t.IsBuyTrade()).ToList();
        List<Trade> sellTrades = trades.Where(t => t.IsSellTrade()).ToList();

        int roundTripTrades = Math.Min(buyTrades.Count, sellTrades.Count);
        int winningTrades = sellTrades.Count(t => t.IsWinningTrade());
        int losingTrades = sellTrades.Count(t => t.IsLosingTrade());
        decimal winRate = sellTrades.Count > 0 ? (decimal)winningTrades / sellTrades.Count * 100 : 0;

        List<decimal> wins = sellTrades.Where(t => t.IsWinningTrade()).Select(t => t.GetProfitLoss()).ToList();
        List<decimal> losses = sellTrades.Where(t => t.IsLosingTrade()).Select(t => t.GetAbsoluteProfitLoss()).ToList();

        decimal averageWin = wins.Count > 0 ? wins.Average() : 0;
        decimal averageLoss = losses.Count > 0 ? losses.Average() : 0;
        decimal largestWin = wins.Count > 0 ? wins.Max() : 0;
        decimal largestLoss = losses.Count > 0 ? losses.Max() : 0;

        decimal grossProfit = wins.Sum();
        decimal grossLoss = losses.Sum();
        decimal profitFactor = grossLoss > 0 ? grossProfit / grossLoss : 0;

        (int maxConsecutiveWins, int maxConsecutiveLosses) = CalculateConsecutiveWinsLosses(sellTrades);

        return new TradeStatistics(
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
            MaxConsecutiveLosses: maxConsecutiveLosses
        );
    }

    /// <summary>
    /// Calculates the maximum consecutive winning and losing streaks.
    /// </summary>
    /// <param name="sellTrades">List of sell trades only.</param>
    /// <returns>Tuple of (max consecutive wins, max consecutive losses).</returns>
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
            if (trade.IsWinningTrade())
            {
                currentWinStreak++;
                currentLossStreak = 0;
                maxWinStreak = Math.Max(maxWinStreak, currentWinStreak);
            }
            else if (trade.IsLosingTrade())
            {
                currentLossStreak++;
                currentWinStreak = 0;
                maxLossStreak = Math.Max(maxLossStreak, currentLossStreak);
            }
        }

        return (maxWinStreak, maxLossStreak);
    }
}

/// <summary>
/// Immutable record containing trade analysis results.
/// </summary>
public record TradeStatistics(
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
    int MaxConsecutiveLosses
);
