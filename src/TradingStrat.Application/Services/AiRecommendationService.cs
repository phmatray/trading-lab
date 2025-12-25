using TradingStrat.Application.Ports.Outbound;
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
        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-daysBack);

        var prices = await _historicalDataPort.GetHistoricalDataAsync(ticker, TimeFrame.D1, startDate, endDate);

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
        (decimal[] macd, decimal[] signal, decimal[] histogram) = _indicatorCalculator.CalculateMACD(closePrices);
        decimal[] atr14 = _indicatorCalculator.CalculateATR(prices.ToArray(), 14);

        var reasons = new List<string>();
        int score = 0; // Range: -100 to +100

        // 1. RSI Analysis (30 points)
        decimal rsiValue = rsi14[latestIdx];
        if (rsiValue < 30)
        {
            score += 30;
            reasons.Add($"RSI ({rsiValue:F1}) indicates oversold conditions - potential buy opportunity");
        }
        else if (rsiValue > 70)
        {
            score -= 30;
            reasons.Add($"RSI ({rsiValue:F1}) indicates overbought conditions - consider taking profits");
        }
        else if (rsiValue < 40)
        {
            score += 15;
            reasons.Add($"RSI ({rsiValue:F1}) shows weak momentum but approaching oversold");
        }
        else if (rsiValue > 60)
        {
            score -= 15;
            reasons.Add($"RSI ({rsiValue:F1}) shows strong momentum but approaching overbought");
        }

        // 2. Moving Average Analysis (40 points)
        decimal currentPrice = closePrices[latestIdx];
        bool priceAboveSma20 = currentPrice > sma20[latestIdx];
        bool priceAboveSma50 = currentPrice > sma50[latestIdx];
        bool sma20AboveSma50 = sma20[latestIdx] > sma50[latestIdx];

        if (priceAboveSma20 && priceAboveSma50 && sma20AboveSma50)
        {
            score += 40;
            reasons.Add("Price above both SMA20 and SMA50 with golden cross - strong uptrend");
        }
        else if (!priceAboveSma20 && !priceAboveSma50 && !sma20AboveSma50)
        {
            score -= 40;
            reasons.Add("Price below both SMA20 and SMA50 with death cross - strong downtrend");
        }
        else if (priceAboveSma20 && !priceAboveSma50)
        {
            score += 20;
            reasons.Add("Price above SMA20 but below SMA50 - short-term strength, long-term weakness");
        }
        else if (!priceAboveSma20 && priceAboveSma50)
        {
            score -= 20;
            reasons.Add("Price below SMA20 but above SMA50 - short-term weakness, long-term support");
        }

        // 3. MACD Analysis (20 points)
        decimal macdValue = histogram[latestIdx];
        if (latestIdx > 0)
        {
            decimal previousMacd = histogram[latestIdx - 1];
            if (macdValue > 0 && previousMacd <= 0)
            {
                score += 20;
                reasons.Add("MACD histogram crossed above zero - bullish momentum shift");
            }
            else if (macdValue < 0 && previousMacd >= 0)
            {
                score -= 20;
                reasons.Add("MACD histogram crossed below zero - bearish momentum shift");
            }
            else if (macdValue > 0)
            {
                score += 10;
                reasons.Add("MACD histogram positive - bullish momentum");
            }
            else if (macdValue < 0)
            {
                score -= 10;
                reasons.Add("MACD histogram negative - bearish momentum");
            }
        }

        // 4. Volatility Check (10 points)
        decimal atrValue = atr14[latestIdx];
        decimal atrPercentOfPrice = currentPrice > 0 ? (atrValue / currentPrice * 100) : 0;
        if (atrPercentOfPrice > 5)
        {
            reasons.Add($"High volatility detected (ATR: {atrPercentOfPrice:F2}% of price) - increased risk");
            score = (int)(score * 0.8m); // Reduce confidence in high volatility
        }

        // Determine action and confidence
        string action;
        int confidence;

        if (score >= 50)
        {
            action = "BUY";
            confidence = Math.Min(50 + score / 2, 95); // Cap at 95%
        }
        else if (score <= -50)
        {
            action = "SELL";
            confidence = Math.Min(50 + Math.Abs(score) / 2, 95);
        }
        else
        {
            action = "HOLD";
            confidence = 50 + (20 - Math.Abs(score) / 5); // Higher confidence for neutral signals close to 0
            reasons.Add("Mixed signals suggest waiting for clearer trend confirmation");
        }

        return new RecommendationResult(action, confidence, reasons);
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
