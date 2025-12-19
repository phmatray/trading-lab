using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.Services.Indicators;

public class IndicatorCalculator : IIndicatorCalculator
{
    public decimal[] CalculateSMA(decimal[] prices, int period)
    {
        ArgumentNullException.ThrowIfNull(prices);
        if (period <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than zero.");
        }

        decimal[] sma = new decimal[prices.Length];

        for (int i = 0; i < prices.Length; i++)
        {
            if (i < period - 1)
            {
                sma[i] = 0;
                continue;
            }

            decimal sum = 0;
            for (int j = 0; j < period; j++)
            {
                sum += prices[i - j];
            }
            sma[i] = sum / period;
        }

        return sma;
    }

    public decimal[] CalculateEMA(decimal[] prices, int period)
    {
        ArgumentNullException.ThrowIfNull(prices);
        if (period <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than zero.");
        }

        decimal[] ema = new decimal[prices.Length];
        decimal multiplier = 2m / (period + 1);

        for (int i = 0; i < prices.Length; i++)
        {
            if (i < period - 1)
            {
                ema[i] = 0;
                continue;
            }

            if (i == period - 1)
            {
                decimal sum = 0;
                for (int j = 0; j < period; j++)
                {
                    sum += prices[i - j];
                }
                ema[i] = sum / period;
            }
            else
            {
                ema[i] = ((prices[i] - ema[i - 1]) * multiplier) + ema[i - 1];
            }
        }

        return ema;
    }

    public decimal[] CalculateRSI(decimal[] prices, int period)
    {
        ArgumentNullException.ThrowIfNull(prices);
        if (period <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than zero.");
        }

        decimal[] rsi = new decimal[prices.Length];

        for (int i = 0; i < prices.Length; i++)
        {
            if (i < period)
            {
                rsi[i] = 50;
                continue;
            }

            (decimal avgGain, decimal avgLoss) = CalculateAverageGainsAndLosses(prices, i, period);
            rsi[i] = CalculateRSIValue(avgGain, avgLoss);
        }

        return rsi;
    }

    private static (decimal avgGain, decimal avgLoss) CalculateAverageGainsAndLosses(decimal[] prices, int index, int period)
    {
        decimal gains = 0;
        decimal losses = 0;

        for (int j = 1; j <= period; j++)
        {
            decimal change = prices[index - j + 1] - prices[index - j];
            if (change > 0)
            {
                gains += change;
            }
            else
            {
                losses -= change;
            }
        }

        return (gains / period, losses / period);
    }

    private static decimal CalculateRSIValue(decimal avgGain, decimal avgLoss)
    {
        if (avgLoss == 0)
        {
            return 100;
        }

        decimal rs = avgGain / avgLoss;
        return 100 - (100 / (1 + rs));
    }

    public (decimal[] macd, decimal[] signal, decimal[] histogram) CalculateMACD(
        decimal[] prices,
        int fastPeriod = 12,
        int slowPeriod = 26,
        int signalPeriod = 9)
    {
        ArgumentNullException.ThrowIfNull(prices);
        if (fastPeriod <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fastPeriod), "Fast period must be greater than zero.");
        }
        if (slowPeriod <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slowPeriod), "Slow period must be greater than zero.");
        }
        if (signalPeriod <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(signalPeriod), "Signal period must be greater than zero.");
        }

        decimal[] fastEMA = CalculateEMA(prices, fastPeriod);
        decimal[] slowEMA = CalculateEMA(prices, slowPeriod);

        decimal[] macd = new decimal[prices.Length];
        for (int i = 0; i < prices.Length; i++)
        {
            macd[i] = fastEMA[i] - slowEMA[i];
        }

        decimal[] signal = CalculateEMAFromArray(macd, signalPeriod);

        decimal[] histogram = new decimal[prices.Length];
        for (int i = 0; i < prices.Length; i++)
        {
            histogram[i] = macd[i] - signal[i];
        }

        return (macd, signal, histogram);
    }

    public decimal[] CalculateATR(HistoricalPrice[] prices, int period)
    {
        ArgumentNullException.ThrowIfNull(prices);
        if (period <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than zero.");
        }

        decimal[] trueRanges = CalculateTrueRanges(prices);
        return CalculateATRFromTrueRanges(trueRanges, period);
    }

    private static decimal[] CalculateTrueRanges(HistoricalPrice[] prices)
    {
        decimal[] trueRanges = new decimal[prices.Length];

        for (int i = 0; i < prices.Length; i++)
        {
            trueRanges[i] = i == 0
                ? CalculateInitialTrueRange(prices[i])
                : CalculateTrueRange(prices[i], prices[i - 1]);
        }

        return trueRanges;
    }

    private static decimal CalculateInitialTrueRange(HistoricalPrice price)
    {
        return (price.High ?? 0) - (price.Low ?? 0);
    }

    private static decimal CalculateTrueRange(HistoricalPrice current, HistoricalPrice previous)
    {
        decimal high = current.High ?? 0;
        decimal low = current.Low ?? 0;
        decimal prevClose = previous.Close ?? 0;

        decimal tr1 = high - low;
        decimal tr2 = Math.Abs(high - prevClose);
        decimal tr3 = Math.Abs(low - prevClose);

        return Math.Max(tr1, Math.Max(tr2, tr3));
    }

    private static decimal[] CalculateATRFromTrueRanges(decimal[] trueRanges, int period)
    {
        decimal[] atr = new decimal[trueRanges.Length];

        for (int i = 0; i < trueRanges.Length; i++)
        {
            if (i < period - 1)
            {
                atr[i] = 0;
                continue;
            }

            atr[i] = i == period - 1
                ? CalculateInitialATR(trueRanges, i, period)
                : CalculateSmoothedATR(atr[i - 1], trueRanges[i], period);
        }

        return atr;
    }

    private static decimal CalculateInitialATR(decimal[] trueRanges, int index, int period)
    {
        decimal sum = 0;
        for (int j = 0; j < period; j++)
        {
            sum += trueRanges[index - j];
        }
        return sum / period;
    }

    private static decimal CalculateSmoothedATR(decimal previousATR, decimal currentTrueRange, int period)
    {
        return ((previousATR * (period - 1)) + currentTrueRange) / period;
    }

    public decimal[] CalculateStdDev(decimal[] prices, int period)
    {
        ArgumentNullException.ThrowIfNull(prices);
        if (period <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than zero.");
        }

        decimal[] stdDev = new decimal[prices.Length];

        for (int i = 0; i < prices.Length; i++)
        {
            if (i < period - 1)
            {
                stdDev[i] = 0;
                continue;
            }

            decimal sum = 0;
            for (int j = 0; j < period; j++)
            {
                sum += prices[i - j];
            }
            decimal mean = sum / period;

            decimal sumSquaredDifferences = 0;
            for (int j = 0; j < period; j++)
            {
                decimal diff = prices[i - j] - mean;
                sumSquaredDifferences += diff * diff;
            }

            decimal variance = sumSquaredDifferences / period;
            stdDev[i] = (decimal)Math.Sqrt((double)variance);
        }

        return stdDev;
    }

    private decimal[] CalculateEMAFromArray(decimal[] data, int period)
    {
        decimal[] ema = new decimal[data.Length];
        decimal multiplier = 2m / (period + 1);

        for (int i = 0; i < data.Length; i++)
        {
            if (i < period - 1)
            {
                ema[i] = 0;
                continue;
            }

            if (i == period - 1)
            {
                decimal sum = 0;
                for (int j = 0; j < period; j++)
                {
                    sum += data[i - j];
                }
                ema[i] = sum / period;
            }
            else
            {
                ema[i] = ((data[i] - ema[i - 1]) * multiplier) + ema[i - 1];
            }
        }

        return ema;
    }

    public decimal[] CalculateTenkan(HistoricalPrice[] prices, int period = 9)
    {
        ArgumentNullException.ThrowIfNull(prices);
        if (period <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than zero.");
        }

        return CalculateDonchianMidpoint(prices, period);
    }

    public decimal[] CalculateKijun(HistoricalPrice[] prices, int period = 26)
    {
        ArgumentNullException.ThrowIfNull(prices);
        if (period <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than zero.");
        }

        return CalculateDonchianMidpoint(prices, period);
    }

    public decimal[] CalculateSenkouSpanA(decimal[] tenkan, decimal[] kijun)
    {
        ArgumentNullException.ThrowIfNull(tenkan);
        ArgumentNullException.ThrowIfNull(kijun);
        if (tenkan.Length != kijun.Length)
        {
            throw new ArgumentException("Tenkan and Kijun arrays must have the same length.");
        }

        decimal[] spanA = new decimal[tenkan.Length];
        for (int i = 0; i < tenkan.Length; i++)
        {
            spanA[i] = (tenkan[i] + kijun[i]) / 2m;
        }

        return spanA;
    }

    public decimal[] CalculateSenkouSpanB(HistoricalPrice[] prices, int period = 52)
    {
        ArgumentNullException.ThrowIfNull(prices);
        if (period <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than zero.");
        }

        return CalculateDonchianMidpoint(prices, period);
    }

    public decimal[] CalculateChikouSpan(HistoricalPrice[] prices)
    {
        ArgumentNullException.ThrowIfNull(prices);
        return prices.Select(p => p.Close ?? 0).ToArray();
    }

    private static decimal[] CalculateDonchianMidpoint(HistoricalPrice[] prices, int period)
    {
        decimal[] result = new decimal[prices.Length];

        for (int i = 0; i < prices.Length; i++)
        {
            if (i < period - 1)
            {
                result[i] = 0;
                continue;
            }

            decimal highestHigh = decimal.MinValue;
            decimal lowestLow = decimal.MaxValue;

            for (int j = 0; j < period; j++)
            {
                decimal high = prices[i - j].High ?? 0;
                decimal low = prices[i - j].Low ?? 0;

                if (high > highestHigh)
                {
                    highestHigh = high;
                }
                if (low < lowestLow)
                {
                    lowestLow = low;
                }
            }

            result[i] = (highestHigh + lowestLow) / 2m;
        }

        return result;
    }
}
