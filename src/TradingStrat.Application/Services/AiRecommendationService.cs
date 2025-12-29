using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Services;

/// <summary>
/// Service for generating AI-powered trading recommendations based on technical analysis.
/// Analyzes market indicators and generates actionable recommendations with confidence scores.
/// </summary>
public class AiRecommendationService
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly IIndicatorCalculator _indicatorCalculator;

    public AiRecommendationService(
        IHistoricalDataPort historicalDataPort,
        IIndicatorCalculator indicatorCalculator)
    {
        _historicalDataPort = historicalDataPort;
        _indicatorCalculator = indicatorCalculator;
    }

    /// <summary>
    /// Generates a trading recommendation for a specific ticker.
    /// </summary>
    /// <param name="ticker">Stock ticker symbol to analyze.</param>
    /// <param name="daysBack">Number of days of historical data to analyze (default 30).</param>
    /// <returns>Recommendation result with action, confidence, and reasons.</returns>
    public async Task<RecommendationResult> GenerateRecommendationAsync(string ticker, int daysBack = 30)
    {
        DateTime endDate = DateTime.Today;
        DateTime startDate = endDate.AddDays(-daysBack);

        List<HistoricalPrice> prices = await _historicalDataPort.GetHistoricalDataAsync(ticker, TimeFrame.D1, startDate, endDate);

        if (prices.Count < 20)
        {
            return new RecommendationResult(
                Action: "HOLD",
                Confidence: 50,
                Reasons: new List<string> { "Insufficient historical data for analysis" }
            );
        }

        decimal[] closePrices = prices.Select(p => p.Close ?? 0).ToArray();
        int latestIdx = closePrices.Length - 1;

        // Calculate indicators
        decimal[] rsi14 = _indicatorCalculator.CalculateRSI(closePrices, 14);
        decimal[] sma20 = _indicatorCalculator.CalculateSMA(closePrices, 20);
        decimal[] sma50 = _indicatorCalculator.CalculateSMA(closePrices, 50);
        (_, _, decimal[] histogram) = _indicatorCalculator.CalculateMACD(closePrices);
        decimal[] atr14 = _indicatorCalculator.CalculateATR(prices.ToArray(), 14);

        var reasons = new List<string>();
        int score = 0;

        // Analyze indicators and accumulate score
        var (rsiScore, rsiReason) = AnalyzeRsiIndicator(rsi14[latestIdx]);
        score += rsiScore;
        if (!string.IsNullOrEmpty(rsiReason))
        {
            reasons.Add(rsiReason);
        }

        decimal currentPrice = closePrices[latestIdx];
        var (maScore, maReason) = AnalyzeMovingAverages(currentPrice, sma20[latestIdx], sma50[latestIdx]);
        score += maScore;
        if (!string.IsNullOrEmpty(maReason))
        {
            reasons.Add(maReason);
        }

        if (latestIdx > 0)
        {
            var (macdScore, macdReason) = AnalyzeMacdIndicator(histogram[latestIdx], histogram[latestIdx - 1]);
            score += macdScore;
            if (!string.IsNullOrEmpty(macdReason))
            {
                reasons.Add(macdReason);
            }
        }

        var (volatilityAdjustedScore, volatilityReason) = AnalyzeVolatility(atr14[latestIdx], currentPrice, score);
        score = volatilityAdjustedScore;
        if (!string.IsNullOrEmpty(volatilityReason))
        {
            reasons.Add(volatilityReason);
        }

        // Determine action and confidence
        var (action, confidence) = DetermineActionAndConfidence(score, reasons);

        return new RecommendationResult(action, confidence, reasons);
    }

    /// <summary>
    /// Analyzes RSI indicator and returns score adjustment and reason.
    /// </summary>
    private static (int ScoreAdjustment, string Reason) AnalyzeRsiIndicator(decimal rsiValue)
    {
        return rsiValue switch
        {
            < 30 => (30, $"RSI ({rsiValue:F1}) indicates oversold conditions - potential buy opportunity"),
            > 70 => (-30, $"RSI ({rsiValue:F1}) indicates overbought conditions - consider taking profits"),
            < 40 => (15, $"RSI ({rsiValue:F1}) shows weak momentum but approaching oversold"),
            > 60 => (-15, $"RSI ({rsiValue:F1}) shows strong momentum but approaching overbought"),
            _ => (0, string.Empty)
        };
    }

    /// <summary>
    /// Analyzes moving averages and returns score adjustment and reason.
    /// </summary>
    private static (int ScoreAdjustment, string Reason) AnalyzeMovingAverages(
        decimal currentPrice,
        decimal sma20,
        decimal sma50)
    {
        bool priceAboveSma20 = currentPrice > sma20;
        bool priceAboveSma50 = currentPrice > sma50;
        bool sma20AboveSma50 = sma20 > sma50;

        return (priceAboveSma20, priceAboveSma50, sma20AboveSma50) switch
        {
            (true, true, true) => (40, "Price above both SMA20 and SMA50 with golden cross - strong uptrend"),
            (false, false, false) => (-40, "Price below both SMA20 and SMA50 with death cross - strong downtrend"),
            (true, false, _) => (20, "Price above SMA20 but below SMA50 - short-term strength, long-term weakness"),
            (false, true, _) => (-20, "Price below SMA20 but above SMA50 - short-term weakness, long-term support"),
            _ => (0, string.Empty)
        };
    }

    /// <summary>
    /// Analyzes MACD histogram and returns score adjustment and reason.
    /// </summary>
    private static (int ScoreAdjustment, string Reason) AnalyzeMacdIndicator(
        decimal macdValue,
        decimal previousMacd)
    {
        return (macdValue, previousMacd) switch
        {
            ( > 0, <= 0) => (20, "MACD histogram crossed above zero - bullish momentum shift"),
            ( < 0, >= 0) => (-20, "MACD histogram crossed below zero - bearish momentum shift"),
            ( > 0, _) => (10, "MACD histogram positive - bullish momentum"),
            ( < 0, _) => (-10, "MACD histogram negative - bearish momentum"),
            _ => (0, string.Empty)
        };
    }

    /// <summary>
    /// Analyzes volatility and adjusts score if volatility is high.
    /// </summary>
    private static (int AdjustedScore, string Reason) AnalyzeVolatility(
        decimal atrValue,
        decimal currentPrice,
        int currentScore)
    {
        decimal atrPercentOfPrice = currentPrice > 0 ? (atrValue / currentPrice * 100) : 0;

        if (atrPercentOfPrice > 5)
        {
            int adjustedScore = (int)(currentScore * 0.8m);
            string reason = $"High volatility detected (ATR: {atrPercentOfPrice:F2}% of price) - increased risk";
            return (adjustedScore, reason);
        }

        return (currentScore, string.Empty);
    }

    /// <summary>
    /// Determines trading action and confidence based on score.
    /// </summary>
    private static (string Action, int Confidence) DetermineActionAndConfidence(int score, List<string> reasons)
    {
        if (score >= 50)
        {
            return ("BUY", Math.Min(50 + score / 2, 95));
        }

        if (score <= -50)
        {
            return ("SELL", Math.Min(50 + Math.Abs(score) / 2, 95));
        }

        reasons.Add("Mixed signals suggest waiting for clearer trend confirmation");
        return ("HOLD", 50 + (20 - Math.Abs(score) / 5));
    }

    /// <summary>
    /// Generates recommendations for all positions in a portfolio.
    /// </summary>
    /// <param name="tickers">List of ticker symbols to analyze.</param>
    /// <param name="daysBack">Number of days of historical data to analyze (default 30).</param>
    /// <returns>Dictionary of ticker to recommendation result.</returns>
    public async Task<Dictionary<string, RecommendationResult>> GeneratePortfolioRecommendationsAsync(
        IEnumerable<string> tickers,
        int daysBack = 30)
    {
        Dictionary<string, RecommendationResult> recommendations = new();

        foreach (string ticker in tickers)
        {
            try
            {
                RecommendationResult recommendation = await GenerateRecommendationAsync(ticker, daysBack);
                recommendations[ticker] = recommendation;
            }
            catch (Exception ex)
            {
                recommendations[ticker] = new RecommendationResult(
                    Action: "HOLD",
                    Confidence: 0,
                    Reasons: new List<string> { $"Error analyzing {ticker}: {ex.Message}" }
                );
            }
        }

        return recommendations;
    }
}

/// <summary>
/// Result of an AI recommendation analysis.
/// </summary>
/// <param name="Action">Recommended action: "BUY", "SELL", or "HOLD".</param>
/// <param name="Confidence">Confidence level (0-100).</param>
/// <param name="Reasons">List of reasons supporting the recommendation.</param>
public record RecommendationResult(
    string Action,
    int Confidence,
    List<string> Reasons
);
