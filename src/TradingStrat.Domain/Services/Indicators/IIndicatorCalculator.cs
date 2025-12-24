using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.Services.Indicators;

/// <summary>
/// Domain service for calculating technical indicators used in trading strategies.
/// Provides 26 different technical indicators including moving averages, momentum, volatility, and volume indicators.
/// This is the single source of truth for all indicator calculations, eliminating duplication.
/// </summary>
public interface IIndicatorCalculator
{
    /// <summary>
    /// Calculates Simple Moving Average (SMA) over a specified period.
    /// SMA is the arithmetic mean of prices over the period.
    /// </summary>
    /// <param name="prices">Price data (typically close prices).</param>
    /// <param name="period">Number of periods to average.</param>
    /// <returns>Array of SMA values, with leading zeros where calculation is not possible.</returns>
    decimal[] CalculateSMA(decimal[] prices, int period);

    /// <summary>
    /// Calculates Exponential Moving Average (EMA) over a specified period.
    /// EMA gives more weight to recent prices compared to SMA.
    /// </summary>
    /// <param name="prices">Price data (typically close prices).</param>
    /// <param name="period">Number of periods for the EMA calculation.</param>
    /// <returns>Array of EMA values, with leading zeros where calculation is not possible.</returns>
    decimal[] CalculateEMA(decimal[] prices, int period);

    /// <summary>
    /// Calculates Relative Strength Index (RSI) to measure momentum.
    /// RSI ranges from 0 to 100, with values above 70 indicating overbought conditions
    /// and values below 30 indicating oversold conditions.
    /// </summary>
    /// <param name="prices">Price data (typically close prices).</param>
    /// <param name="period">Number of periods for RSI calculation (commonly 14).</param>
    /// <returns>Array of RSI values (0-100), with leading zeros where calculation is not possible.</returns>
    decimal[] CalculateRSI(decimal[] prices, int period);

    /// <summary>
    /// Calculates Moving Average Convergence Divergence (MACD) indicator.
    /// MACD shows the relationship between two moving averages and is used to identify trend changes.
    /// </summary>
    /// <param name="prices">Price data (typically close prices).</param>
    /// <param name="fastPeriod">Period for fast EMA (default 12).</param>
    /// <param name="slowPeriod">Period for slow EMA (default 26).</param>
    /// <param name="signalPeriod">Period for signal line EMA (default 9).</param>
    /// <returns>Tuple containing MACD line, signal line, and histogram values.</returns>
    (decimal[] macd, decimal[] signal, decimal[] histogram) CalculateMACD(
        decimal[] prices,
        int fastPeriod = 12,
        int slowPeriod = 26,
        int signalPeriod = 9);

    /// <summary>
    /// Calculates Average True Range (ATR) to measure market volatility.
    /// ATR considers gaps and limit moves to provide a complete picture of volatility.
    /// </summary>
    /// <param name="prices">Historical price data including open, high, low, and close.</param>
    /// <param name="period">Number of periods for ATR calculation (commonly 14).</param>
    /// <returns>Array of ATR values, with leading zeros where calculation is not possible.</returns>
    decimal[] CalculateATR(HistoricalPrice[] prices, int period);

    /// <summary>
    /// Calculates Standard Deviation of prices over a specified period.
    /// Used to measure volatility and price dispersion.
    /// </summary>
    /// <param name="prices">Price data.</param>
    /// <param name="period">Number of periods for standard deviation calculation.</param>
    /// <returns>Array of standard deviation values, with leading zeros where calculation is not possible.</returns>
    decimal[] CalculateStdDev(decimal[] prices, int period);

    /// <summary>
    /// Calculates Conversion Line (Tenkan-sen) - midpoint of highest high and lowest low over period.
    /// Formula: (Highest High + Lowest Low) / 2 over period bars
    /// </summary>
    /// <param name="prices">Historical price data including high and low.</param>
    /// <param name="period">Period for calculation (standard: 9).</param>
    /// <returns>Array of Conversion Line values (unshifted), with leading zeros where calculation is not possible.</returns>
    decimal[] CalculateConversionLine(HistoricalPrice[] prices, int period = 9);

    /// <summary>
    /// Calculates Base Line (Kijun-sen) - midpoint of highest high and lowest low over period.
    /// Formula: (Highest High + Lowest Low) / 2 over period bars
    /// </summary>
    /// <param name="prices">Historical price data including high and low.</param>
    /// <param name="period">Period for calculation (standard: 26).</param>
    /// <returns>Array of Base Line values (unshifted), with leading zeros where calculation is not possible.</returns>
    decimal[] CalculateBaseLine(HistoricalPrice[] prices, int period = 26);

    /// <summary>
    /// Calculates Leading Span A (Senkou Span A) - midpoint of Conversion Line and Base Line.
    /// Formula: (Conversion Line + Base Line) / 2
    /// Note: Traditionally shifted +26 periods for display, but this method returns unshifted values.
    /// Strategy implementation handles shifting.
    /// </summary>
    /// <param name="conversionLine">Conversion Line (Tenkan-sen) values.</param>
    /// <param name="baseLine">Base Line (Kijun-sen) values.</param>
    /// <returns>Array of Leading Span A values (unshifted).</returns>
    decimal[] CalculateLeadingSpanA(decimal[] conversionLine, decimal[] baseLine);

    /// <summary>
    /// Calculates Leading Span B (Senkou Span B) - midpoint of highest high and lowest low over period.
    /// Formula: (Highest High + Lowest Low) / 2 over period bars
    /// Note: Traditionally shifted +26 periods for display, but this method returns unshifted values.
    /// Strategy implementation handles shifting.
    /// </summary>
    /// <param name="prices">Historical price data including high and low.</param>
    /// <param name="period">Period for calculation (standard: 52).</param>
    /// <returns>Array of Leading Span B values (unshifted), with leading zeros where calculation is not possible.</returns>
    decimal[] CalculateLeadingSpanB(HistoricalPrice[] prices, int period = 52);

    /// <summary>
    /// Calculates Lagging Span (Chikou Span) - current close price.
    /// Note: Traditionally shifted -26 periods for display, but this method returns unshifted close prices.
    /// Strategy implementation handles shifting.
    /// </summary>
    /// <param name="prices">Historical price data.</param>
    /// <returns>Array of close prices (unshifted).</returns>
    decimal[] CalculateLaggingSpan(HistoricalPrice[] prices);
}
