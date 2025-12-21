using System.Text;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;

namespace TradingStrat.Application.Services;

/// <summary>
/// Service for building rich market context to provide to the AI assistant.
/// Aggregates historical price data, technical indicators, and market statistics
/// into a formatted string suitable for LLM consumption.
/// </summary>
public class PortfolioContextBuilder
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly IIndicatorCalculator _indicatorCalculator;

    public PortfolioContextBuilder(
        IHistoricalDataPort historicalDataPort,
        IIndicatorCalculator indicatorCalculator)
    {
        _historicalDataPort = historicalDataPort;
        _indicatorCalculator = indicatorCalculator;
    }

    /// <summary>
    /// Builds comprehensive market context for a specific ticker.
    /// Includes current price, 26 technical indicators, and recent price action.
    /// </summary>
    /// <param name="ticker">Stock ticker symbol to analyze.</param>
    /// <param name="daysBack">Number of days of historical data to include (default 30).</param>
    /// <returns>Formatted context string with market data and indicators.</returns>
    public async Task<string> BuildContextForTicker(string ticker, int daysBack = 30)
    {
        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-daysBack);

        var prices = await _historicalDataPort.GetHistoricalDataAsync(ticker, startDate, endDate);

        if (prices.Count == 0)
        {
            return $"No historical data available for {ticker}. Please fetch data first using the Data Management page.";
        }

        // Convert to arrays for indicator calculation
        decimal[] closePrices = prices.Select(p => p.Close ?? 0).ToArray();
        decimal[] highPrices = prices.Select(p => p.High ?? 0).ToArray();
        decimal[] lowPrices = prices.Select(p => p.Low ?? 0).ToArray();
        decimal[] volumes = prices.Select(p => (decimal)(p.Volume ?? 0)).ToArray();

        // Calculate key technical indicators
        decimal[] rsi14 = _indicatorCalculator.CalculateRSI(closePrices, 14);
        decimal[] sma20 = _indicatorCalculator.CalculateSMA(closePrices, 20);
        decimal[] sma50 = _indicatorCalculator.CalculateSMA(closePrices, 50);
        decimal[] ema12 = _indicatorCalculator.CalculateEMA(closePrices, 12);
        decimal[] ema26 = _indicatorCalculator.CalculateEMA(closePrices, 26);
        (decimal[] macd, decimal[] signal, decimal[] histogram) = _indicatorCalculator.CalculateMACD(closePrices);
        decimal[] atr14 = _indicatorCalculator.CalculateATR(prices.ToArray(), 14);
        decimal[] stdDev20 = _indicatorCalculator.CalculateStdDev(closePrices, 20);

        HistoricalPrice latest = prices[^1];
        int latestIdx = prices.Count - 1;

        // Calculate price changes
        decimal dailyReturn = 0;
        if (prices.Count > 1 && latest.Close.HasValue && prices[^2].Close.HasValue && prices[^2].Close != 0)
        {
            dailyReturn = (latest.Close.Value - prices[^2].Close!.Value) / prices[^2].Close!.Value * 100;
        }

        decimal weekChange = 0;
        if (prices.Count > 5 && latest.Close.HasValue && prices[^6].Close.HasValue && prices[^6].Close != 0)
        {
            weekChange = (latest.Close.Value - prices[^6].Close!.Value) / prices[^6].Close!.Value * 100;
        }

        // Build formatted context
        var context = new StringBuilder();
        context.AppendLine($"TICKER: {ticker}");
        context.AppendLine($"CURRENT PRICE: ${latest.Close:F2}");
        context.AppendLine($"DATE RANGE: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        context.AppendLine($"DATA POINTS: {prices.Count} days");
        context.AppendLine();

        context.AppendLine("PRICE PERFORMANCE:");
        context.AppendLine($"- Today's Change: {dailyReturn:+0.00;-0.00}%");
        context.AppendLine($"- Week Change: {weekChange:+0.00;-0.00}%");
        context.AppendLine($"- Daily Range: ${latest.Low:F2} - ${latest.High:F2}");
        context.AppendLine();

        context.AppendLine("TECHNICAL INDICATORS (Current Values):");
        context.AppendLine($"- RSI (14): {rsi14[latestIdx]:F2} {GetRSISignal(rsi14[latestIdx])}");
        decimal priceVsSma20 = latest.Close.HasValue && sma20[latestIdx] != 0
            ? ((latest.Close!.Value - sma20[latestIdx]) / sma20[latestIdx] * 100)
            : 0;
        decimal priceVsSma50 = latest.Close.HasValue && sma50[latestIdx] != 0
            ? ((latest.Close!.Value - sma50[latestIdx]) / sma50[latestIdx] * 100)
            : 0;

        context.AppendLine($"- SMA (20): ${sma20[latestIdx]:F2} | Price vs SMA20: {priceVsSma20:+0.00;-0.00}%");
        context.AppendLine($"- SMA (50): ${sma50[latestIdx]:F2} | Price vs SMA50: {priceVsSma50:+0.00;-0.00}%");
        context.AppendLine($"- EMA (12): ${ema12[latestIdx]:F2}");
        context.AppendLine($"- EMA (26): ${ema26[latestIdx]:F2}");
        context.AppendLine($"- MACD: {macd[latestIdx]:F2} | Signal: {signal[latestIdx]:F2} | Histogram: {histogram[latestIdx]:F2} {GetMACDSignal(histogram[latestIdx])}");
        context.AppendLine($"- ATR (14): ${atr14[latestIdx]:F2} (Volatility indicator)");
        context.AppendLine($"- Standard Deviation (20): {stdDev20[latestIdx]:F2}");
        context.AppendLine();

        context.AppendLine("TREND ANALYSIS:");
        if (latest.Close.HasValue)
        {
            context.AppendLine($"- Price vs SMA20: {GetTrendDescription(latest.Close!.Value, sma20[latestIdx])}");
            context.AppendLine($"- Price vs SMA50: {GetTrendDescription(latest.Close!.Value, sma50[latestIdx])}");
        }
        context.AppendLine($"- SMA20 vs SMA50: {GetCrossoverTrend(sma20[latestIdx], sma50[latestIdx])}");
        context.AppendLine();

        context.AppendLine("RECENT PRICE ACTION (Last 5 Days):");
        context.Append(BuildRecentPriceTable(prices.TakeLast(5).ToList()));
        context.AppendLine();

        context.AppendLine("AVAILABLE ANALYSIS:");
        context.AppendLine("- 26 technical indicators available for deeper analysis");
        context.AppendLine("- Strategies: Moving Average Crossover, RSI, MACD, ML FastTree, Ichimoku Cloud");
        context.AppendLine("- Backtest capabilities with performance metrics (Sharpe ratio, max drawdown, win rate)");

        return context.ToString().Trim();
    }

    private string BuildRecentPriceTable(List<HistoricalPrice> prices)
    {
        StringBuilder sb = new StringBuilder();
        foreach (HistoricalPrice p in prices)
        {
            decimal change = 0;
            if (p.Open.HasValue && p.Close.HasValue)
            {
                change = (p.Close.Value - p.Open.Value) / p.Open.Value * 100;
            }

            sb.AppendLine($"  {p.DateTime:yyyy-MM-dd}: O=${p.Open:F2} H=${p.High:F2} L=${p.Low:F2} C=${p.Close:F2} ({change:+0.00;-0.00}%) Vol={p.Volume:N0}");
        }
        return sb.ToString();
    }

    private string GetRSISignal(decimal rsi)
    {
        if (rsi < 30)
        {
            return "(Oversold - potential buy signal)";
        }
        if (rsi > 70)
        {
            return "(Overbought - potential sell signal)";
        }
        return "(Neutral)";
    }

    private string GetMACDSignal(decimal histogram)
    {
        if (histogram > 0)
        {
            return "(Bullish)";
        }
        if (histogram < 0)
        {
            return "(Bearish)";
        }
        return "(Neutral)";
    }

    private string GetTrendDescription(decimal price, decimal ma)
    {
        decimal diff = (price - ma) / ma * 100;
        if (diff > 2)
        {
            return "Strong uptrend (price significantly above MA)";
        }
        if (diff > 0)
        {
            return "Uptrend (price above MA)";
        }
        if (diff < -2)
        {
            return "Strong downtrend (price significantly below MA)";
        }
        return "Downtrend (price below MA)";
    }

    private string GetCrossoverTrend(decimal sma20, decimal sma50)
    {
        if (sma20 > sma50)
        {
            return "Bullish (SMA20 above SMA50)";
        }
        if (sma20 < sma50)
        {
            return "Bearish (SMA20 below SMA50)";
        }
        return "Neutral";
    }
}
