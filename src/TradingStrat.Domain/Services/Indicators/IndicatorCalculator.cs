using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.Services.Indicators;

public class IndicatorCalculator : IIndicatorCalculator
{
    public decimal[] CalculateSMA(decimal[] prices, int period)
    {
        var sma = new decimal[prices.Length];

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
        var ema = new decimal[prices.Length];
        var multiplier = 2m / (period + 1);

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
        var rsi = new decimal[prices.Length];

        for (int i = 0; i < prices.Length; i++)
        {
            if (i < period)
            {
                rsi[i] = 50;
                continue;
            }

            decimal gains = 0;
            decimal losses = 0;

            for (int j = 1; j <= period; j++)
            {
                var change = prices[i - j + 1] - prices[i - j];
                if (change > 0)
                    gains += change;
                else
                    losses -= change;
            }

            var avgGain = gains / period;
            var avgLoss = losses / period;

            if (avgLoss == 0)
            {
                rsi[i] = 100;
            }
            else
            {
                var rs = avgGain / avgLoss;
                rsi[i] = 100 - (100 / (1 + rs));
            }
        }

        return rsi;
    }

    public (decimal[] macd, decimal[] signal, decimal[] histogram) CalculateMACD(
        decimal[] prices,
        int fastPeriod = 12,
        int slowPeriod = 26,
        int signalPeriod = 9)
    {
        var fastEMA = CalculateEMA(prices, fastPeriod);
        var slowEMA = CalculateEMA(prices, slowPeriod);

        var macd = new decimal[prices.Length];
        for (int i = 0; i < prices.Length; i++)
        {
            macd[i] = fastEMA[i] - slowEMA[i];
        }

        var signal = CalculateEMAFromArray(macd, signalPeriod);

        var histogram = new decimal[prices.Length];
        for (int i = 0; i < prices.Length; i++)
        {
            histogram[i] = macd[i] - signal[i];
        }

        return (macd, signal, histogram);
    }

    public decimal[] CalculateATR(HistoricalPrice[] prices, int period)
    {
        var atr = new decimal[prices.Length];
        var trueRanges = new decimal[prices.Length];

        for (int i = 0; i < prices.Length; i++)
        {
            if (i == 0)
            {
                trueRanges[i] = (prices[i].High ?? 0) - (prices[i].Low ?? 0);
            }
            else
            {
                var high = prices[i].High ?? 0;
                var low = prices[i].Low ?? 0;
                var prevClose = prices[i - 1].Close ?? 0;

                var tr1 = high - low;
                var tr2 = Math.Abs(high - prevClose);
                var tr3 = Math.Abs(low - prevClose);

                trueRanges[i] = Math.Max(tr1, Math.Max(tr2, tr3));
            }
        }

        for (int i = 0; i < prices.Length; i++)
        {
            if (i < period - 1)
            {
                atr[i] = 0;
                continue;
            }

            if (i == period - 1)
            {
                decimal sum = 0;
                for (int j = 0; j < period; j++)
                {
                    sum += trueRanges[i - j];
                }
                atr[i] = sum / period;
            }
            else
            {
                atr[i] = ((atr[i - 1] * (period - 1)) + trueRanges[i]) / period;
            }
        }

        return atr;
    }

    public decimal[] CalculateStdDev(decimal[] prices, int period)
    {
        var stdDev = new decimal[prices.Length];

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
            var mean = sum / period;

            decimal sumSquaredDifferences = 0;
            for (int j = 0; j < period; j++)
            {
                var diff = prices[i - j] - mean;
                sumSquaredDifferences += diff * diff;
            }

            var variance = sumSquaredDifferences / period;
            stdDev[i] = (decimal)Math.Sqrt((double)variance);
        }

        return stdDev;
    }

    private decimal[] CalculateEMAFromArray(decimal[] data, int period)
    {
        var ema = new decimal[data.Length];
        var multiplier = 2m / (period + 1);

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
}
