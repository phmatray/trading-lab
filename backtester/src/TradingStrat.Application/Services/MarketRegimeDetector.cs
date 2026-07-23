using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Services;

/// <summary>
/// Service for detecting market regime (BULLISH, BEARISH, NEUTRAL) based on technical indicators.
/// Analyzes multiple indicators to determine overall market trend and sentiment.
/// </summary>
public class MarketRegimeDetector
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly IIndicatorCalculator _indicatorCalculator;

    public MarketRegimeDetector(
        IHistoricalDataPort historicalDataPort,
        IIndicatorCalculator indicatorCalculator)
    {
        _historicalDataPort = historicalDataPort;
        _indicatorCalculator = indicatorCalculator;
    }

    /// <summary>
    /// Detects the market regime for a specific ticker.
    /// </summary>
    /// <param name="ticker">Stock ticker symbol to analyze.</param>
    /// <param name="daysBack">Number of days of historical data to analyze (default 60).</param>
    /// <returns>Market regime: "BULLISH", "BEARISH", or "NEUTRAL".</returns>
    public async Task<string> DetectRegimeAsync(string ticker, int daysBack = 60)
    {
        DateTime endDate = DateTime.Today;
        DateTime startDate = endDate.AddDays(-daysBack);

        List<HistoricalPrice> prices = await _historicalDataPort.GetHistoricalDataAsync(ticker, TimeFrame.D1, startDate, endDate);

        if (prices.Count < 50)
        {
            return "NEUTRAL"; // Insufficient data
        }

        decimal[] closePrices = prices.Select(p => p.Close ?? 0).ToArray();
        int latestIdx = closePrices.Length - 1;

        // Calculate key indicators
        decimal[] sma20 = _indicatorCalculator.CalculateSMA(closePrices, 20);
        decimal[] sma50 = _indicatorCalculator.CalculateSMA(closePrices, 50);
        decimal[] rsi14 = _indicatorCalculator.CalculateRSI(closePrices, 14);
        (_, _, decimal[] histogram) = _indicatorCalculator.CalculateMACD(closePrices);

        // Score based on multiple indicators (range: -100 to +100)
        int score = 0;

        // 1. Price vs SMA (40 points max)
        if (closePrices[latestIdx] > sma20[latestIdx])
        {
            score += 20;
        }
        else
        {
            score -= 20;
        }

        if (closePrices[latestIdx] > sma50[latestIdx])
        {
            score += 20;
        }
        else
        {
            score -= 20;
        }

        // 2. SMA crossover (30 points max)
        if (sma20[latestIdx] > sma50[latestIdx])
        {
            score += 30; // Golden cross territory
        }
        else
        {
            score -= 30; // Death cross territory
        }

        // 3. RSI (20 points max)
        if (rsi14[latestIdx] > 60)
        {
            score += 20; // Strong momentum
        }
        else if (rsi14[latestIdx] < 40)
        {
            score -= 20; // Weak momentum
        }

        // 4. MACD (10 points max)
        if (histogram[latestIdx] > 0)
        {
            score += 10; // Bullish MACD
        }
        else
        {
            score -= 10; // Bearish MACD
        }

        // Determine regime based on score
        if (score >= 30)
        {
            return "BULLISH";
        }
        if (score <= -30)
        {
            return "BEARISH";
        }
        return "NEUTRAL";
    }

    /// <summary>
    /// Detects overall market regime based on portfolio holdings.
    /// Analyzes all positions and returns the consensus regime.
    /// </summary>
    /// <param name="tickers">List of ticker symbols in the portfolio.</param>
    /// <param name="daysBack">Number of days of historical data to analyze (default 60).</param>
    /// <returns>Overall market regime: "BULLISH", "BEARISH", or "NEUTRAL".</returns>
    public async Task<string> DetectPortfolioRegimeAsync(IEnumerable<string> tickers, int daysBack = 60)
    {
        List<string> regimes = new();

        foreach (string ticker in tickers)
        {
            try
            {
                string regime = await DetectRegimeAsync(ticker, daysBack);
                regimes.Add(regime);
            }
            catch
            {
                // Skip tickers with missing data
            }
        }

        if (regimes.Count == 0)
        {
            return "NEUTRAL";
        }

        // Calculate consensus
        int bullishCount = regimes.Count(r => r == "BULLISH");
        int bearishCount = regimes.Count(r => r == "BEARISH");

        int total = regimes.Count;
        decimal bullishPct = (decimal)bullishCount / total;
        decimal bearishPct = (decimal)bearishCount / total;

        // Require >50% consensus for directional regime
        if (bullishPct > 0.5m)
        {
            return "BULLISH";
        }
        if (bearishPct > 0.5m)
        {
            return "BEARISH";
        }
        return "NEUTRAL";
    }
}
