using TradingStrat.Models;

namespace TradingStrat.Services.Strategies;

public abstract class BaseStrategy : IStrategy
{
    protected IReadOnlyList<HistoricalPrice> HistoricalData { get; private set; } = null!;
    protected decimal[] ClosePrices { get; private set; } = null!;

    public abstract string Name { get; }
    public abstract string Description { get; }

    public virtual void Initialize(IReadOnlyList<HistoricalPrice> historicalData)
    {
        HistoricalData = historicalData;
        ClosePrices = historicalData.Select(h => h.Close ?? 0).ToArray();
    }

    public abstract TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition);

    public abstract Dictionary<string, object> GetParameters();

    protected decimal[] CalculateSMA(int period)
    {
        var sma = new decimal[ClosePrices.Length];

        for (int i = 0; i < ClosePrices.Length; i++)
        {
            if (i < period - 1)
            {
                sma[i] = 0;
                continue;
            }

            decimal sum = 0;
            for (int j = 0; j < period; j++)
            {
                sum += ClosePrices[i - j];
            }
            sma[i] = sum / period;
        }

        return sma;
    }

    protected decimal[] CalculateEMA(int period)
    {
        var ema = new decimal[ClosePrices.Length];
        var multiplier = 2m / (period + 1);

        for (int i = 0; i < ClosePrices.Length; i++)
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
                    sum += ClosePrices[i - j];
                }
                ema[i] = sum / period;
            }
            else
            {
                ema[i] = ((ClosePrices[i] - ema[i - 1]) * multiplier) + ema[i - 1];
            }
        }

        return ema;
    }

    protected decimal[] CalculateRSI(int period)
    {
        var rsi = new decimal[ClosePrices.Length];

        for (int i = 0; i < ClosePrices.Length; i++)
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
                var change = ClosePrices[i - j + 1] - ClosePrices[i - j];
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

    protected (decimal[] macd, decimal[] signal, decimal[] histogram) CalculateMACD(
        int fastPeriod = 12,
        int slowPeriod = 26,
        int signalPeriod = 9)
    {
        var fastEMA = CalculateEMA(fastPeriod);
        var slowEMA = CalculateEMA(slowPeriod);

        var macd = new decimal[ClosePrices.Length];
        for (int i = 0; i < ClosePrices.Length; i++)
        {
            macd[i] = fastEMA[i] - slowEMA[i];
        }

        var signal = CalculateEMAFromArray(macd, signalPeriod);

        var histogram = new decimal[ClosePrices.Length];
        for (int i = 0; i < ClosePrices.Length; i++)
        {
            histogram[i] = macd[i] - signal[i];
        }

        return (macd, signal, histogram);
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

    protected int CalculateQuantity(decimal cash, decimal price, int currentPosition)
    {
        if (currentPosition > 0)
            return 0;

        return (int)(cash / price);
    }
}
