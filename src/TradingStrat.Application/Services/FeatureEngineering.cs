using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Services;

public class FeatureEngineering
{
    private readonly IReadOnlyList<HistoricalPrice> _historicalData;
    private readonly IIndicatorCalculator _indicatorCalculator;
    private readonly decimal[] _closePrices;
    private readonly decimal[] _openPrices;
    private readonly decimal[] _highPrices;
    private readonly decimal[] _lowPrices;
    private readonly long[] _volumes;

    // Cached indicators (calculated once)
    private decimal[] _sma5 = null!;
    private decimal[] _sma10 = null!;
    private decimal[] _sma20 = null!;
    private decimal[] _ema12 = null!;
    private decimal[] _ema26 = null!;
    private decimal[] _rsi14 = null!;
    private decimal[] _macd = null!;
    private decimal[] _macdSignal = null!;
    private decimal[] _macdHistogram = null!;
    private decimal[] _stdDev10 = null!;
    private decimal[] _stdDev20 = null!;
    private decimal[] _atr14 = null!;
    private decimal[] _volumeMA10 = null!;

    public FeatureEngineering(
        IReadOnlyList<HistoricalPrice> historicalData,
        IIndicatorCalculator indicatorCalculator)
    {
        _historicalData = historicalData;
        _indicatorCalculator = indicatorCalculator;
        _closePrices = historicalData.Select(h => h.Close ?? 0).ToArray();

        // Extract OHLCV arrays
        _openPrices = historicalData.Select(h => h.Open ?? 0).ToArray();
        _highPrices = historicalData.Select(h => h.High ?? 0).ToArray();
        _lowPrices = historicalData.Select(h => h.Low ?? 0).ToArray();
        _volumes = historicalData.Select(h => h.Volume ?? 0).ToArray();

        // Calculate all indicators once during initialization
        CalculateAllIndicators();
    }

    private void CalculateAllIndicators()
    {
        // Use IIndicatorCalculator instead of BaseStrategy
        _sma5 = _indicatorCalculator.CalculateSMA(_closePrices, 5);
        _sma10 = _indicatorCalculator.CalculateSMA(_closePrices, 10);
        _sma20 = _indicatorCalculator.CalculateSMA(_closePrices, 20);
        _ema12 = _indicatorCalculator.CalculateEMA(_closePrices, 12);
        _ema26 = _indicatorCalculator.CalculateEMA(_closePrices, 26);
        _rsi14 = _indicatorCalculator.CalculateRSI(_closePrices, 14);
        (_macd, _macdSignal, _macdHistogram) = _indicatorCalculator.CalculateMACD(_closePrices);

        // Use IIndicatorCalculator for these as well
        _stdDev10 = CalculateReturnStdDev(10);
        _stdDev20 = CalculateReturnStdDev(20);
        _atr14 = _indicatorCalculator.CalculateATR(_historicalData.ToArray(), 14);
        _volumeMA10 = CalculateVolumeSMA(10);
    }

    public MarketFeatures[] BuildFeatureMatrix()
    {
        var features = new MarketFeatures[_closePrices.Length];

        for (int i = 0; i < _closePrices.Length; i++)
        {
            features[i] = BuildFeaturesForIndex(i);
        }

        return features;
    }

    public MarketFeatures BuildFeaturesForIndex(int index)
    {
        if (index < 1)
        {
            return CreateDefaultFeatures();
        }

        return new MarketFeatures
        {
            // Price-based (5)
            DailyReturn = CalculateSafeReturn(index),
            LogReturn = CalculateSafeLogReturn(index),
            HighLowRange = CalculateHighLowRange(index),
            OpenCloseRange = CalculateOpenCloseRange(index),
            PricePosition = CalculatePricePosition(index),

            // Moving averages (6)
            SMA_5 = (float)_sma5[index],
            SMA_10 = (float)_sma10[index],
            SMA_20 = (float)_sma20[index],
            EMA_12 = (float)_ema12[index],
            EMA_26 = (float)_ema26[index],
            PriceToSMA20 = CalculatePriceToSMA20(index),

            // Momentum (4)
            RSI_14 = (float)_rsi14[index],
            Momentum_5 = CalculateMomentum(index, 5),
            ROC_10 = CalculateROC(index, 10),
            StochRSI = CalculateStochRSI(index),

            // MACD (3)
            MACD = (float)_macd[index],
            MACDSignal = (float)_macdSignal[index],
            MACDHistogram = (float)_macdHistogram[index],

            // Volatility (4)
            StdDev_10 = (float)_stdDev10[index],
            StdDev_20 = (float)_stdDev20[index],
            ATR_14 = (float)_atr14[index],
            BollingerPosition = CalculateBollingerPosition(index),

            // Volume (4)
            VolumeChange = CalculateVolumeChange(index),
            VolumeMA_10 = (float)_volumeMA10[index],
            VolumeRatio = CalculateVolumeRatio(index),
            PriceVolumeCorrelation = CalculatePriceVolumeCorrelation(index, 10),

            // Target (only for training, set to 0 for last bar)
            NextDayReturn = CalculateNextDayReturn(index)
        };
    }

    private MarketFeatures CreateDefaultFeatures()
    {
        return new MarketFeatures();
    }

    // Price-based features
    private float CalculateSafeReturn(int index)
    {
        if (index < 1 || _closePrices[index - 1] == 0)
        {
            return 0;
        }

        return ValidateFloat((float)((_closePrices[index] - _closePrices[index - 1]) / _closePrices[index - 1]));
    }

    private float CalculateSafeLogReturn(int index)
    {
        if (index < 1 || _closePrices[index - 1] == 0 || _closePrices[index] == 0)
        {
            return 0;
        }

        return ValidateFloat((float)Math.Log((double)(_closePrices[index] / _closePrices[index - 1])));
    }

    private float CalculateHighLowRange(int index)
    {
        if (_closePrices[index] == 0 || _highPrices[index] == _lowPrices[index])
        {
            return 0;
        }

        return ValidateFloat((float)((_highPrices[index] - _lowPrices[index]) / _closePrices[index]));
    }

    private float CalculateOpenCloseRange(int index)
    {
        if (_openPrices[index] == 0)
        {
            return 0;
        }

        return ValidateFloat((float)((_closePrices[index] - _openPrices[index]) / _openPrices[index]));
    }

    private float CalculatePricePosition(int index)
    {
        decimal range = _highPrices[index] - _lowPrices[index];
        if (range == 0)
        {
            return 0.5f;
        }

        return ValidateFloat((float)((_closePrices[index] - _lowPrices[index]) / range));
    }

    // Moving average features
    private float CalculatePriceToSMA20(int index)
    {
        if (_sma20[index] == 0)
        {
            return 0;
        }

        return ValidateFloat((float)((_closePrices[index] - _sma20[index]) / _sma20[index]));
    }

    // Momentum features
    private float CalculateMomentum(int index, int period)
    {
        if (index < period)
        {
            return 0;
        }

        return ValidateFloat((float)(_closePrices[index] - _closePrices[index - period]));
    }

    private float CalculateROC(int index, int period)
    {
        if (index < period || _closePrices[index - period] == 0)
        {
            return 0;
        }

        return ValidateFloat((float)((_closePrices[index] - _closePrices[index - period]) / _closePrices[index - period]));
    }

    private float CalculateStochRSI(int index)
    {
        if (index < 14)
        {
            return 50;
        }

        int period = 14;
        decimal minRSI = decimal.MaxValue;
        decimal maxRSI = decimal.MinValue;

        for (int i = Math.Max(0, index - period + 1); i <= index; i++)
        {
            if (_rsi14[i] < minRSI)
            {
                minRSI = _rsi14[i];
            }

            if (_rsi14[i] > maxRSI)
            {
                maxRSI = _rsi14[i];
            }
        }

        if (maxRSI == minRSI)
        {
            return 50;
        }

        return ValidateFloat((float)(((_rsi14[index] - minRSI) / (maxRSI - minRSI)) * 100));
    }

    // Volatility features
    private float CalculateBollingerPosition(int index)
    {
        if (index < 20 || _sma20[index] == 0 || _stdDev20[index] == 0)
        {
            return 0.5f;
        }

        decimal upperBand = _sma20[index] + (2 * _stdDev20[index]);
        decimal lowerBand = _sma20[index] - (2 * _stdDev20[index]);
        decimal bandWidth = upperBand - lowerBand;

        if (bandWidth == 0)
        {
            return 0.5f;
        }

        return ValidateFloat((float)((_closePrices[index] - lowerBand) / bandWidth));
    }

    // Volume features
    private float CalculateVolumeChange(int index)
    {
        if (index < 1 || _volumes[index - 1] == 0)
        {
            return 0;
        }

        return ValidateFloat((float)((_volumes[index] - _volumes[index - 1]) / (double)_volumes[index - 1]));
    }

    private float CalculateVolumeRatio(int index)
    {
        if (_volumeMA10[index] == 0)
        {
            return 1;
        }

        return ValidateFloat((float)(_volumes[index] / (double)_volumeMA10[index]));
    }

    private float CalculatePriceVolumeCorrelation(int index, int period)
    {
        if (index < period)
        {
            return 0;
        }

        var priceChanges = new List<double>();
        var volumeChanges = new List<double>();

        for (int i = index - period + 1; i <= index; i++)
        {
            if (i < 1)
            {
                continue;
            }

            if (_closePrices[i - 1] != 0)
            {
                priceChanges.Add((double)((_closePrices[i] - _closePrices[i - 1]) / _closePrices[i - 1]));
            }

            if (_volumes[i - 1] != 0)
            {
                volumeChanges.Add((_volumes[i] - _volumes[i - 1]) / (double)_volumes[i - 1]);
            }
        }

        if (priceChanges.Count < 2)
        {
            return 0;
        }

        return ValidateFloat(CalculateCorrelation(priceChanges, volumeChanges));
    }

    // Target variable
    private float CalculateNextDayReturn(int index)
    {
        if (index >= _closePrices.Length - 1 || _closePrices[index] == 0)
        {
            return 0;
        }

        return ValidateFloat((float)((_closePrices[index + 1] - _closePrices[index]) / _closePrices[index]));
    }

    // Helper methods
    private decimal[] CalculateVolumeSMA(int period)
    {
        decimal[] volumeMA = new decimal[_volumes.Length];

        for (int i = 0; i < _volumes.Length; i++)
        {
            if (i < period - 1)
            {
                volumeMA[i] = 0;
                continue;
            }

            decimal sum = 0;
            for (int j = 0; j < period; j++)
            {
                sum += _volumes[i - j];
            }
            volumeMA[i] = sum / period;
        }

        return volumeMA;
    }

    private float CalculateCorrelation(List<double> x, List<double> y)
    {
        if (x.Count != y.Count || x.Count < 2)
        {
            return 0;
        }

        int n = x.Count;
        double sumX = x.Sum();
        double sumY = y.Sum();
        double sumXY = x.Zip(y, (a, b) => a * b).Sum();
        double sumX2 = x.Sum(a => a * a);
        double sumY2 = y.Sum(b => b * b);

        double numerator = (n * sumXY) - (sumX * sumY);
        double denominator = Math.Sqrt(((n * sumX2) - (sumX * sumX)) * ((n * sumY2) - (sumY * sumY)));

        if (denominator == 0)
        {
            return 0;
        }

        return (float)(numerator / denominator);
    }

    private float ValidateFloat(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
    }

    private decimal[] CalculateReturnStdDev(int period)
    {
        decimal[] stdDev = new decimal[_closePrices.Length];

        for (int i = 0; i < _closePrices.Length; i++)
        {
            if (i < period)
            {
                stdDev[i] = 0;
                continue;
            }

            List<decimal> returns = CalculateReturnsForPeriod(i, period);

            if (returns.Count < 2)
            {
                stdDev[i] = 0;
                continue;
            }

            stdDev[i] = CalculateStandardDeviation(returns);
        }

        return stdDev;
    }

    private List<decimal> CalculateReturnsForPeriod(int endIndex, int period)
    {
        var returns = new List<decimal>();

        for (int j = endIndex - period + 1; j <= endIndex; j++)
        {
            if (j > 0 && _closePrices[j - 1] != 0)
            {
                returns.Add((_closePrices[j] - _closePrices[j - 1]) / _closePrices[j - 1]);
            }
        }

        return returns;
    }

    private static decimal CalculateStandardDeviation(List<decimal> values)
    {
        decimal mean = values.Average();
        decimal variance = values.Sum(r => (r - mean) * (r - mean)) / (values.Count - 1);
        return (decimal)Math.Sqrt((double)variance);
    }
}
