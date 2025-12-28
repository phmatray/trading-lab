using System.Text;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Services;

/// <summary>
/// Service for building rich market context to provide to the AI assistant.
/// Aggregates historical price data, technical indicators, portfolio holdings, and market statistics
/// into a formatted string suitable for LLM consumption.
/// </summary>
public class PortfolioContextBuilder
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly IIndicatorCalculator _indicatorCalculator;
    private readonly IPortfolioPort _portfolioPort;
    private readonly PortfolioValuationService _valuationService;

    public PortfolioContextBuilder(
        IHistoricalDataPort historicalDataPort,
        IIndicatorCalculator indicatorCalculator,
        IPortfolioPort portfolioPort,
        PortfolioValuationService valuationService)
    {
        _historicalDataPort = historicalDataPort;
        _indicatorCalculator = indicatorCalculator;
        _portfolioPort = portfolioPort;
        _valuationService = valuationService;
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
        DateTime endDate = DateTime.Today;
        DateTime startDate = endDate.AddDays(-daysBack);

        List<HistoricalPrice> prices = await _historicalDataPort.GetHistoricalDataAsync(ticker, TimeFrame.D1, startDate, endDate);

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
        if (prices.Count > 1 && latest.Close is { } currentClose && prices[^2].Close is { } previousClose && previousClose != 0)
        {
            dailyReturn = (currentClose - previousClose) / previousClose * 100;
        }

        decimal weekChange = 0;
        if (prices.Count > 5 && latest.Close is { } currentClose2 && prices[^6].Close is { } weekAgoClose && weekAgoClose != 0)
        {
            weekChange = (currentClose2 - weekAgoClose) / weekAgoClose * 100;
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

    /// <summary>
    /// Builds comprehensive portfolio context including all holdings, valuations, and technical analysis.
    /// Includes portfolio summary, position details, and technical indicators for each holding.
    /// </summary>
    /// <param name="portfolioId">Portfolio ID to analyze.</param>
    /// <param name="daysBack">Number of days of historical data to include (default 30).</param>
    /// <returns>Formatted context string with portfolio data and market analysis.</returns>
    public async Task<string> BuildContextForPortfolio(int portfolioId, int daysBack = 30)
    {
        Portfolio? portfolio = await _portfolioPort.GetPortfolioByIdAsync(portfolioId);
        if (portfolio is null)
        {
            return $"Portfolio with ID {portfolioId} not found.";
        }

        var context = new StringBuilder();
        context.AppendLine($"PORTFOLIO: {portfolio.Name}");
        if (!string.IsNullOrEmpty(portfolio.Description))
        {
            context.AppendLine($"DESCRIPTION: {portfolio.Description}");
        }
        context.AppendLine($"CASH BALANCE: ${portfolio.Cash:N2}");
        context.AppendLine($"NUMBER OF POSITIONS: {portfolio.Positions.Count}");
        context.AppendLine($"LAST UPDATED: {portfolio.LastUpdated:yyyy-MM-dd HH:mm:ss}");
        context.AppendLine();

        if (portfolio.Positions.Count == 0)
        {
            context.AppendLine("No positions in portfolio.");
            return context.ToString().Trim();
        }

        // Fetch current prices for all positions
        var currentPrices = new Dictionary<string, decimal>();
        foreach (Position position in portfolio.Positions)
        {
            try
            {
                DateTime endDate = DateTime.Today;
                DateTime startDate = endDate.AddDays(-daysBack);
                List<HistoricalPrice> prices = await _historicalDataPort.GetHistoricalDataAsync(
                    position.Ticker,
                    TimeFrame.D1,
                    startDate,
                    endDate);

                if (prices.Count > 0)
                {
                    currentPrices[position.Ticker] = prices[^1].Close ?? 0;
                }
            }
            catch
            {
                // Skip positions with missing data
            }
        }

        // Calculate portfolio snapshot
        Result<PortfolioSnapshot> snapshotResult = _valuationService.CalculateSnapshot(portfolio, currentPrices);
        if (snapshotResult.IsSuccess)
        {
            PortfolioSnapshot snapshot = snapshotResult.Value;
            decimal cashAllocation = snapshot.TotalValue > 0 ? (snapshot.Cash / snapshot.TotalValue * 100) : 0;

            context.AppendLine("PORTFOLIO SUMMARY:");
            context.AppendLine($"- Total Value: ${snapshot.TotalValue:N2}");
            context.AppendLine($"- Total Cost Basis: ${snapshot.TotalCost:N2}");
            context.AppendLine($"- Unrealized Gain/Loss: ${snapshot.UnrealizedGainLoss:N2} ({snapshot.UnrealizedGainLossPercentage:+0.00;-0.00}%)");
            context.AppendLine($"- Cash Allocation: {cashAllocation:F2}%");
            context.AppendLine();

            context.AppendLine("POSITIONS:");
            foreach (PositionSnapshot posSnapshot in snapshot.Positions)
            {
                context.AppendLine($"\n{posSnapshot.Ticker}:");
                context.AppendLine($"  Quantity: {posSnapshot.Quantity}");
                context.AppendLine($"  Entry Price: ${posSnapshot.EntryPrice:F2}");
                context.AppendLine($"  Current Price: ${posSnapshot.CurrentPrice:F2}");
                context.AppendLine($"  Market Value: ${posSnapshot.MarketValue:N2}");
                context.AppendLine($"  Cost Basis: ${posSnapshot.CostBasis:N2}");
                context.AppendLine($"  Gain/Loss: ${posSnapshot.UnrealizedGainLoss:N2} ({posSnapshot.UnrealizedGainLossPercentage:+0.00;-0.00}%)");
                context.AppendLine($"  Portfolio Allocation: {posSnapshot.AllocationPercentage:F2}%");
            }
            context.AppendLine();
        }

        // Add technical analysis for top 3 holdings by allocation
        var topHoldings = portfolio.Positions
            .Where(p => currentPrices.ContainsKey(p.Ticker))
            .OrderByDescending(p => p.Quantity * currentPrices[p.Ticker])
            .Take(3)
            .ToList();

        if (topHoldings.Any())
        {
            context.AppendLine("TOP HOLDINGS TECHNICAL ANALYSIS:");
            context.AppendLine();

            foreach (Position position in topHoldings)
            {
                try
                {
                    string tickerContext = await BuildContextForTicker(position.Ticker, daysBack);
                    context.AppendLine(tickerContext);
                    context.AppendLine();
                }
                catch (Exception ex)
                {
                    context.AppendLine($"{position.Ticker}: Unable to load technical analysis ({ex.Message})");
                    context.AppendLine();
                }
            }
        }

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
