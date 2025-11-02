// <copyright file="IndicatorLibrary.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.MarketData;

namespace TradingBot.Strategies.Indicators;

/// <summary>
/// Technical indicator library for calculating common trading indicators.
/// </summary>
public static class IndicatorLibrary
{
    /// <summary>
    /// Calculates Simple Moving Average (SMA).
    /// </summary>
    /// <param name="candles">Historical candle data.</param>
    /// <param name="period">Number of periods for the average.</param>
    /// <returns>SMA value.</returns>
    /// <exception cref="ArgumentException">Thrown when insufficient data.</exception>
    public static decimal CalculateSMA(IReadOnlyList<Candle> candles, int period)
    {
        if (candles.Count < period)
        {
            throw new ArgumentException($"Insufficient data: need {period} candles, got {candles.Count}");
        }

        var recentCandles = candles.TakeLast(period);
        return recentCandles.Average(c => c.Close);
    }

    /// <summary>
    /// Calculates Exponential Moving Average (EMA).
    /// </summary>
    /// <param name="candles">Historical candle data.</param>
    /// <param name="period">Number of periods for the average.</param>
    /// <returns>EMA value.</returns>
    /// <exception cref="ArgumentException">Thrown when insufficient data.</exception>
    public static decimal CalculateEMA(IReadOnlyList<Candle> candles, int period)
    {
        if (candles.Count < period)
        {
            throw new ArgumentException($"Insufficient data: need {period} candles, got {candles.Count}");
        }

        var multiplier = 2m / (period + 1);
        var ema = candles.Take(period).Average(c => c.Close); // Start with SMA

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
    /// <param name="period">Number of periods (typically 14).</param>
    /// <returns>RSI value (0-100).</returns>
    /// <exception cref="ArgumentException">Thrown when insufficient data.</exception>
    public static decimal CalculateRSI(IReadOnlyList<Candle> candles, int period)
    {
        if (candles.Count < period + 1)
        {
            throw new ArgumentException($"Insufficient data: need {period + 1} candles, got {candles.Count}");
        }

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (int i = 1; i < candles.Count; i++)
        {
            var change = candles[i].Close - candles[i - 1].Close;
            gains.Add(change > 0 ? change : 0);
            losses.Add(change < 0 ? -change : 0);
        }

        var avgGain = gains.TakeLast(period).Average();
        var avgLoss = losses.TakeLast(period).Average();

        if (avgLoss == 0)
        {
            return 100m;
        }

        var rs = avgGain / avgLoss;
        return 100m - (100m / (1m + rs));
    }

    /// <summary>
    /// Calculates Moving Average Convergence Divergence (MACD).
    /// </summary>
    /// <param name="candles">Historical candle data.</param>
    /// <param name="fastPeriod">Fast EMA period (typically 12).</param>
    /// <param name="slowPeriod">Slow EMA period (typically 26).</param>
    /// <param name="signalPeriod">Signal line period (typically 9).</param>
    /// <returns>Tuple containing MACD line, signal line, and histogram.</returns>
    /// <exception cref="ArgumentException">Thrown when insufficient data.</exception>
    public static (decimal Macd, decimal Signal, decimal Histogram) CalculateMACD(
        IReadOnlyList<Candle> candles,
        int fastPeriod = 12,
        int slowPeriod = 26,
        int signalPeriod = 9)
    {
        if (candles.Count < slowPeriod + signalPeriod)
        {
            throw new ArgumentException(
                $"Insufficient data: need {slowPeriod + signalPeriod} candles, got {candles.Count}");
        }

        var fastEma = CalculateEMA(candles, fastPeriod);
        var slowEma = CalculateEMA(candles, slowPeriod);
        var macdLine = fastEma - slowEma;

        // Calculate signal line (EMA of MACD)
        var macdValues = new List<decimal>();
        for (int i = slowPeriod; i < candles.Count; i++)
        {
            var subset = candles.Take(i + 1).ToList();
            var fast = CalculateEMA(subset, fastPeriod);
            var slow = CalculateEMA(subset, slowPeriod);
            macdValues.Add(fast - slow);
        }

        var signalLine = CalculateEMAFromValues(macdValues, signalPeriod);
        var histogram = macdLine - signalLine;

        return (macdLine, signalLine, histogram);
    }

    /// <summary>
    /// Calculates Bollinger Bands.
    /// </summary>
    /// <param name="candles">Historical candle data.</param>
    /// <param name="period">Number of periods (typically 20).</param>
    /// <param name="stdDevMultiplier">Standard deviation multiplier (typically 2.0).</param>
    /// <returns>Tuple containing upper band, middle band (SMA), and lower band.</returns>
    /// <exception cref="ArgumentException">Thrown when insufficient data.</exception>
    public static (decimal Upper, decimal Middle, decimal Lower) CalculateBollingerBands(
        IReadOnlyList<Candle> candles,
        int period = 20,
        double stdDevMultiplier = 2.0)
    {
        if (candles.Count < period)
        {
            throw new ArgumentException($"Insufficient data: need {period} candles, got {candles.Count}");
        }

        var middle = CalculateSMA(candles, period);
        var recentPrices = candles.TakeLast(period).Select(c => c.Close).ToList();

        // Calculate standard deviation
        var avg = (double)middle;
        var sumSquares = recentPrices.Sum(p => Math.Pow((double)p - avg, 2));
        var variance = sumSquares / period;
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
    /// <param name="period">Number of periods (typically 14).</param>
    /// <returns>ATR value.</returns>
    /// <exception cref="ArgumentException">Thrown when insufficient data.</exception>
    public static decimal CalculateATR(IReadOnlyList<Candle> candles, int period = 14)
    {
        if (candles.Count < period + 1)
        {
            throw new ArgumentException($"Insufficient data: need {period + 1} candles, got {candles.Count}");
        }

        var trueRanges = new List<decimal>();

        for (int i = 1; i < candles.Count; i++)
        {
            var current = candles[i];
            var previous = candles[i - 1];

            var tr1 = current.High - current.Low;
            var tr2 = Math.Abs(current.High - previous.Close);
            var tr3 = Math.Abs(current.Low - previous.Close);

            var trueRange = Math.Max(tr1, Math.Max(tr2, tr3));
            trueRanges.Add(trueRange);
        }

        return trueRanges.TakeLast(period).Average();
    }

    /// <summary>
    /// Helper method to calculate EMA from a list of decimal values.
    /// </summary>
    /// <param name="values">List of values.</param>
    /// <param name="period">EMA period.</param>
    /// <returns>EMA value.</returns>
    private static decimal CalculateEMAFromValues(IReadOnlyList<decimal> values, int period)
    {
        if (values.Count < period)
        {
            throw new ArgumentException($"Insufficient data: need {period} values, got {values.Count}");
        }

        var multiplier = 2m / (period + 1);
        var ema = values.Take(period).Average(); // Start with SMA

        foreach (var value in values.Skip(period))
        {
            ema = ((value - ema) * multiplier) + ema;
        }

        return ema;
    }
}
