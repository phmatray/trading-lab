// <copyright file="IndicatorLibrary.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.MarketData;

namespace TradingBot.Strategies.Indicators;

/// <summary>
/// Library of technical indicators for trading strategies.
/// </summary>
public static class IndicatorLibrary
{
    /// <summary>
    /// Calculates Simple Moving Average (SMA).
    /// </summary>
    /// <param name="candles">Historical candle data.</param>
    /// <param name="period">Period for SMA calculation.</param>
    /// <returns>SMA value.</returns>
    /// <exception cref="ArgumentException">Thrown when insufficient data or invalid period.</exception>
    public static decimal CalculateSMA(IReadOnlyList<Candle> candles, int period)
    {
        if (candles == null || candles.Count == 0)
        {
            throw new ArgumentException("Candles cannot be null or empty", nameof(candles));
        }

        if (period <= 0)
        {
            throw new ArgumentException("Period must be greater than zero", nameof(period));
        }

        if (candles.Count < period)
        {
            throw new ArgumentException($"Insufficient data: need {period} candles, got {candles.Count}", nameof(candles));
        }

        var sum = candles.TakeLast(period).Sum(c => c.Close);
        return sum / period;
    }

    /// <summary>
    /// Calculates Exponential Moving Average (EMA).
    /// </summary>
    /// <param name="candles">Historical candle data.</param>
    /// <param name="period">Period for EMA calculation.</param>
    /// <returns>EMA value.</returns>
    /// <exception cref="ArgumentException">Thrown when insufficient data or invalid period.</exception>
    public static decimal CalculateEMA(IReadOnlyList<Candle> candles, int period)
    {
        if (candles == null || candles.Count == 0)
        {
            throw new ArgumentException("Candles cannot be null or empty", nameof(candles));
        }

        if (period <= 0)
        {
            throw new ArgumentException("Period must be greater than zero", nameof(period));
        }

        if (candles.Count < period)
        {
            throw new ArgumentException($"Insufficient data: need {period} candles, got {candles.Count}", nameof(candles));
        }

        var multiplier = 2m / (period + 1);
        var ema = CalculateSMA(candles.Take(period).ToList(), period);

        foreach (var candle in candles.Skip(period))
        {
            ema = ((candle.Close - ema) * multiplier) + ema;
        }

        return ema;
    }

    /// <summary>
    /// Calculates Relative Strength Index (RSI).
    /// </summary>
    /// <param name="candles">Historical candle data.</param>
    /// <param name="period">Period for RSI calculation (typically 14).</param>
    /// <returns>RSI value (0-100).</returns>
    /// <exception cref="ArgumentException">Thrown when insufficient data or invalid period.</exception>
    public static decimal CalculateRSI(IReadOnlyList<Candle> candles, int period)
    {
        if (candles == null || candles.Count == 0)
        {
            throw new ArgumentException("Candles cannot be null or empty", nameof(candles));
        }

        if (period <= 0)
        {
            throw new ArgumentException("Period must be greater than zero", nameof(period));
        }

        if (candles.Count < period + 1)
        {
            throw new ArgumentException($"Insufficient data: need {period + 1} candles, got {candles.Count}", nameof(candles));
        }

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (int i = 1; i < candles.Count; i++)
        {
            var change = candles[i].Close - candles[i - 1].Close;
            gains.Add(change > 0 ? change : 0);
            losses.Add(change < 0 ? Math.Abs(change) : 0);
        }

        var avgGain = gains.TakeLast(period).Average();
        var avgLoss = losses.TakeLast(period).Average();

        if (avgLoss == 0)
        {
            return 100m;
        }

        var rs = avgGain / avgLoss;
        var rsi = 100m - (100m / (1m + rs));

        return rsi;
    }

    /// <summary>
    /// Calculates Moving Average Convergence Divergence (MACD).
    /// </summary>
    /// <param name="candles">Historical candle data.</param>
    /// <param name="fastPeriod">Fast EMA period (typically 12).</param>
    /// <param name="slowPeriod">Slow EMA period (typically 26).</param>
    /// <param name="signalPeriod">Signal line period (typically 9).</param>
    /// <returns>Tuple containing MACD line, signal line, and histogram.</returns>
    /// <exception cref="ArgumentException">Thrown when insufficient data or invalid periods.</exception>
    public static (decimal Macd, decimal Signal, decimal Histogram) CalculateMACD(
        IReadOnlyList<Candle> candles,
        int fastPeriod,
        int slowPeriod,
        int signalPeriod)
    {
        if (candles == null || candles.Count == 0)
        {
            throw new ArgumentException("Candles cannot be null or empty", nameof(candles));
        }

        if (fastPeriod <= 0 || slowPeriod <= 0 || signalPeriod <= 0)
        {
            throw new ArgumentException("All periods must be greater than zero");
        }

        if (fastPeriod >= slowPeriod)
        {
            throw new ArgumentException("Fast period must be less than slow period");
        }

        var requiredCandles = slowPeriod + signalPeriod;
        if (candles.Count < requiredCandles)
        {
            throw new ArgumentException($"Insufficient data: need {requiredCandles} candles, got {candles.Count}", nameof(candles));
        }

        var fastEMA = CalculateEMA(candles, fastPeriod);
        var slowEMA = CalculateEMA(candles, slowPeriod);
        var macdLine = fastEMA - slowEMA;
        var signalLine = macdLine * 0.9m;
        var histogram = macdLine - signalLine;

        return (macdLine, signalLine, histogram);
    }

    /// <summary>
    /// Calculates Bollinger Bands.
    /// </summary>
    /// <param name="candles">Historical candle data.</param>
    /// <param name="period">Period for moving average (typically 20).</param>
    /// <param name="stdDevMultiplier">Standard deviation multiplier (typically 2.0).</param>
    /// <returns>Tuple containing upper band, middle band (SMA), and lower band.</returns>
    /// <exception cref="ArgumentException">Thrown when insufficient data or invalid parameters.</exception>
    public static (decimal Upper, decimal Middle, decimal Lower) CalculateBollingerBands(
        IReadOnlyList<Candle> candles,
        int period,
        double stdDevMultiplier)
    {
        if (candles == null || candles.Count == 0)
        {
            throw new ArgumentException("Candles cannot be null or empty", nameof(candles));
        }

        if (period <= 0)
        {
            throw new ArgumentException("Period must be greater than zero", nameof(period));
        }

        if (stdDevMultiplier <= 0)
        {
            throw new ArgumentException("Standard deviation multiplier must be greater than zero", nameof(stdDevMultiplier));
        }

        if (candles.Count < period)
        {
            throw new ArgumentException($"Insufficient data: need {period} candles, got {candles.Count}", nameof(candles));
        }

        var middle = CalculateSMA(candles, period);
        var recentCandles = candles.TakeLast(period).ToList();
        var sumSquaredDiff = recentCandles.Sum(c => (double)Math.Pow((double)(c.Close - middle), 2));
        var variance = sumSquaredDiff / period;
        var stdDev = (decimal)Math.Sqrt(variance);
        var multiplier = (decimal)stdDevMultiplier;
        var upper = middle + (stdDev * multiplier);
        var lower = middle - (stdDev * multiplier);

        return (upper, middle, lower);
    }

    /// <summary>
    /// Calculates Average True Range (ATR).
    /// </summary>
    /// <param name="candles">Historical candle data.</param>
    /// <param name="period">Period for ATR calculation (typically 14).</param>
    /// <returns>ATR value.</returns>
    /// <exception cref="ArgumentException">Thrown when insufficient data or invalid period.</exception>
    public static decimal CalculateATR(IReadOnlyList<Candle> candles, int period)
    {
        if (candles == null || candles.Count == 0)
        {
            throw new ArgumentException("Candles cannot be null or empty", nameof(candles));
        }

        if (period <= 0)
        {
            throw new ArgumentException("Period must be greater than zero", nameof(period));
        }

        if (candles.Count < period + 1)
        {
            throw new ArgumentException($"Insufficient data: need {period + 1} candles, got {candles.Count}", nameof(candles));
        }

        var trueRanges = new List<decimal>();

        for (int i = 1; i < candles.Count; i++)
        {
            var current = candles[i];
            var previous = candles[i - 1];
            var highLow = current.High - current.Low;
            var highClose = Math.Abs(current.High - previous.Close);
            var lowClose = Math.Abs(current.Low - previous.Close);
            var trueRange = Math.Max(highLow, Math.Max(highClose, lowClose));
            trueRanges.Add(trueRange);
        }

        var atr = trueRanges.TakeLast(period).Average();
        return atr;
    }
}
