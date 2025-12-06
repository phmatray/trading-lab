using Microsoft.Extensions.Logging;
using Microsoft.ML;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Strategies;

/// <summary>
/// Machine Learning strategy using walk-forward validation for backtesting.
/// Trains a new model at each time step using only historical data available up to that point.
/// </summary>
public class MachineLearningStrategy : BaseStrategy
{
    private readonly IIndicatorCalculator _indicatorCalculator;
    private readonly PredictionThresholds _thresholds;
    private readonly ILogger<MachineLearningStrategy>? _logger;
    private readonly MLContext _mlContext;
    private readonly int _minTrainingBars = 100;

    private decimal[] _highPrices = null!;
    private decimal[] _lowPrices = null!;
    private decimal[] _openPrices = null!;
    private long[] _volumes = null!;

    public override string Name => "ML FastTree (Walk-Forward)";

    public override string Description =>
        "Machine learning strategy using FastTree gradient boosting with walk-forward validation. " +
        "Trains a new model at each time step to avoid look-ahead bias. " +
        "Note: This is computationally expensive as it trains hundreds of models during backtesting.";

    public MachineLearningStrategy(
        IIndicatorCalculator indicatorCalculator,
        PredictionThresholds? thresholds = null,
        ILogger<MachineLearningStrategy>? logger = null)
        : base(indicatorCalculator)
    {
        _indicatorCalculator = indicatorCalculator;
        _thresholds = thresholds ?? new PredictionThresholds(0.01m, -0.01m);
        _logger = logger;
        _mlContext = new MLContext(seed: 42);
    }

    public override void Initialize(IReadOnlyList<HistoricalPrice> historicalData)
    {
        base.Initialize(historicalData);

        _highPrices = historicalData.Select(h => h.High ?? 0).ToArray();
        _lowPrices = historicalData.Select(h => h.Low ?? 0).ToArray();
        _openPrices = historicalData.Select(h => h.Open ?? 0).ToArray();
        _volumes = historicalData.Select(h => h.Volume ?? 0).ToArray();

        _logger?.LogDebug("Initialized ML strategy with {DataPoints} data points, min training bars: {MinBars}",
            historicalData.Count, _minTrainingBars);
    }

    public override TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition)
    {
        decimal currentPrice = ClosePrices[currentIndex];

        // Need minimum training data
        if (currentIndex < _minTrainingBars)
        {
            _logger?.LogDebug("Insufficient training data at index {Index} (need {MinBars})", currentIndex, _minTrainingBars);
            return new TradeSignal(SignalType.Hold, currentPrice, 0, "Insufficient training data");
        }

        try
        {
            // Build features for training (all data up to currentIndex)
            var features = BuildFeatures(currentIndex + 1);

            // Train model with walk-forward approach (exclude last feature which is for prediction)
            var model = TrainModel(features, currentIndex);

            // Predict for current bar (last feature in the array)
            var currentFeature = features[^1];
            float predictedReturn = Predict(model, currentFeature);

            _logger?.LogDebug("Prediction at index {Index}: {Return:F4}", currentIndex, predictedReturn);

            // Convert prediction to signal
            return GenerateSignalFromPrediction(predictedReturn, currentPrice, currentCash, currentPosition);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generating ML signal at index {Index}", currentIndex);
            return new TradeSignal(SignalType.Hold, currentPrice, 0, $"ML error: {ex.Message}");
        }
    }

    private MarketFeatures[] BuildFeatures(int upToIndex)
    {
        var features = new List<MarketFeatures>();

        for (int i = 30; i < upToIndex; i++)
        {
            var feature = new MarketFeatures
            {
                // Price-based (5)
                DailyReturn = CalculateDailyReturn(i),
                LogReturn = CalculateLogReturn(i),
                HighLowRange = CalculateHighLowRange(i),
                OpenCloseRange = CalculateOpenCloseRange(i),
                PricePosition = CalculatePricePosition(i),

                // Moving averages (6)
                SMA_5 = (float)CalculateSMA(5)[i],
                SMA_10 = (float)CalculateSMA(10)[i],
                SMA_20 = (float)CalculateSMA(20)[i],
                EMA_12 = (float)CalculateEMA(12)[i],
                EMA_26 = (float)CalculateEMA(26)[i],
                PriceToSMA20 = CalculatePriceToSMA20(i),

                // Momentum (4)
                RSI_14 = (float)CalculateRSI(14)[i],
                Momentum_5 = CalculateMomentum(i, 5),
                ROC_10 = CalculateROC(i, 10),
                StochRSI = CalculateStochRSI(i),

                // MACD (3)
                MACD = (float)CalculateMACD().macd[i],
                MACDSignal = (float)CalculateMACD().signal[i],
                MACDHistogram = (float)CalculateMACD().histogram[i],

                // Volatility (4)
                StdDev_10 = CalculateReturnStdDev(i, 10),
                StdDev_20 = CalculateReturnStdDev(i, 20),
                ATR_14 = (float)_indicatorCalculator.CalculateATR(HistoricalData.ToArray(), 14)[i],
                BollingerPosition = CalculateBollingerPosition(i),

                // Volume (4)
                VolumeChange = CalculateVolumeChange(i),
                VolumeMA_10 = CalculateVolumeMA(i, 10),
                VolumeRatio = CalculateVolumeRatio(i),
                PriceVolumeCorrelation = CalculatePriceVolumeCorrelation(i, 10),

                // Target (next day return)
                NextDayReturn = i < upToIndex - 1 ? CalculateDailyReturn(i + 1) : 0f
            };

            features.Add(feature);
        }

        return features.ToArray();
    }

    private ITransformer TrainModel(MarketFeatures[] features, int currentIndex)
    {
        // Exclude the last feature (current bar) from training since it has no target
        var trainingFeatures = features.Take(features.Length - 1).ToArray();
        var trainingData = _mlContext.Data.LoadFromEnumerable(trainingFeatures);

        _logger?.LogDebug("Training model with {Count} samples", trainingFeatures.Length);

        var pipeline = _mlContext.Transforms
            .Concatenate("Features",
                nameof(MarketFeatures.DailyReturn), nameof(MarketFeatures.LogReturn),
                nameof(MarketFeatures.HighLowRange), nameof(MarketFeatures.OpenCloseRange),
                nameof(MarketFeatures.PricePosition),
                nameof(MarketFeatures.SMA_5), nameof(MarketFeatures.SMA_10),
                nameof(MarketFeatures.SMA_20), nameof(MarketFeatures.EMA_12),
                nameof(MarketFeatures.EMA_26), nameof(MarketFeatures.PriceToSMA20),
                nameof(MarketFeatures.RSI_14), nameof(MarketFeatures.Momentum_5),
                nameof(MarketFeatures.ROC_10), nameof(MarketFeatures.StochRSI),
                nameof(MarketFeatures.MACD), nameof(MarketFeatures.MACDSignal),
                nameof(MarketFeatures.MACDHistogram),
                nameof(MarketFeatures.StdDev_10), nameof(MarketFeatures.StdDev_20),
                nameof(MarketFeatures.ATR_14), nameof(MarketFeatures.BollingerPosition),
                nameof(MarketFeatures.VolumeChange), nameof(MarketFeatures.VolumeMA_10),
                nameof(MarketFeatures.VolumeRatio), nameof(MarketFeatures.PriceVolumeCorrelation))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Regression.Trainers.FastTree(
                labelColumnName: nameof(MarketFeatures.NextDayReturn),
                featureColumnName: "Features",
                numberOfLeaves: 31,
                minimumExampleCountPerLeaf: 20,
                learningRate: 0.1,
                numberOfTrees: 100));

        return pipeline.Fit(trainingData);
    }

    private float Predict(ITransformer model, MarketFeatures features)
    {
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<MarketFeatures, PricePrediction>(model);
        var prediction = predictionEngine.Predict(features);
        return prediction.Score;
    }

    private TradeSignal GenerateSignalFromPrediction(float predictedReturn, decimal currentPrice, decimal currentCash, int currentPosition)
    {
        decimal returnDecimal = (decimal)predictedReturn;

        if (returnDecimal >= _thresholds.BuyThreshold && currentPosition == 0)
        {
            int quantity = CalculateQuantity(currentCash, currentPrice, currentPosition);
            if (quantity > 0)
            {
                return new TradeSignal(
                    SignalType.Buy,
                    currentPrice,
                    quantity,
                    $"ML predicts +{predictedReturn:P2} return");
            }
        }
        else if (returnDecimal <= _thresholds.SellThreshold && currentPosition > 0)
        {
            return new TradeSignal(
                SignalType.Sell,
                currentPrice,
                currentPosition,
                $"ML predicts {predictedReturn:P2} return");
        }

        return new TradeSignal(SignalType.Hold, currentPrice, 0, $"ML predicts {predictedReturn:P2}, holding");
    }

    // Feature calculation helper methods
    private float CalculateDailyReturn(int index)
    {
        if (index < 1 || ClosePrices[index - 1] == 0)
        {
            return 0f;
        }

        return (float)((ClosePrices[index] - ClosePrices[index - 1]) / ClosePrices[index - 1]);
    }

    private float CalculateLogReturn(int index)
    {
        if (index < 1 || ClosePrices[index - 1] == 0 || ClosePrices[index] == 0)
        {
            return 0f;
        }

        return (float)Math.Log((double)(ClosePrices[index] / ClosePrices[index - 1]));
    }

    private float CalculateHighLowRange(int index)
    {
        if (ClosePrices[index] == 0 || _highPrices[index] == _lowPrices[index])
        {
            return 0f;
        }

        return (float)((_highPrices[index] - _lowPrices[index]) / ClosePrices[index]);
    }

    private float CalculateOpenCloseRange(int index)
    {
        if (_openPrices[index] == 0)
        {
            return 0f;
        }

        return (float)((ClosePrices[index] - _openPrices[index]) / _openPrices[index]);
    }

    private float CalculatePricePosition(int index)
    {
        decimal range = _highPrices[index] - _lowPrices[index];
        if (range == 0)
        {
            return 0.5f;
        }

        return (float)((ClosePrices[index] - _lowPrices[index]) / range);
    }

    private float CalculatePriceToSMA20(int index)
    {
        decimal[] sma20 = CalculateSMA(20);
        if (sma20[index] == 0)
        {
            return 0f;
        }

        return (float)((ClosePrices[index] - sma20[index]) / sma20[index]);
    }

    private float CalculateMomentum(int index, int period)
    {
        if (index < period)
        {
            return 0f;
        }

        return (float)(ClosePrices[index] - ClosePrices[index - period]);
    }

    private float CalculateROC(int index, int period)
    {
        if (index < period || ClosePrices[index - period] == 0)
        {
            return 0f;
        }

        return (float)((ClosePrices[index] - ClosePrices[index - period]) / ClosePrices[index - period]);
    }

    private float CalculateStochRSI(int index)
    {
        if (index < 14)
        {
            return 50f;
        }

        decimal[] rsi14 = CalculateRSI(14);
        int period = 14;
        decimal minRSI = decimal.MaxValue;
        decimal maxRSI = decimal.MinValue;

        for (int i = Math.Max(0, index - period + 1); i <= index; i++)
        {
            if (rsi14[i] < minRSI)
            {
                minRSI = rsi14[i];
            }

            if (rsi14[i] > maxRSI)
            {
                maxRSI = rsi14[i];
            }
        }

        if (maxRSI == minRSI)
        {
            return 50f;
        }

        return (float)(((rsi14[index] - minRSI) / (maxRSI - minRSI)) * 100);
    }

    private float CalculateReturnStdDev(int index, int period)
    {
        if (index < period)
        {
            return 0f;
        }

        var returns = new List<decimal>();
        for (int j = index - period + 1; j <= index; j++)
        {
            if (j > 0 && ClosePrices[j - 1] != 0)
            {
                returns.Add((ClosePrices[j] - ClosePrices[j - 1]) / ClosePrices[j - 1]);
            }
        }

        if (returns.Count < 2)
        {
            return 0f;
        }

        decimal mean = returns.Average();
        decimal variance = returns.Sum(r => (r - mean) * (r - mean)) / (returns.Count - 1);
        return (float)Math.Sqrt((double)variance);
    }

    private float CalculateBollingerPosition(int index)
    {
        if (index < 20)
        {
            return 0.5f;
        }

        decimal[] sma20 = CalculateSMA(20);
        float stdDev20 = CalculateReturnStdDev(index, 20);

        if (sma20[index] == 0 || stdDev20 == 0)
        {
            return 0.5f;
        }

        decimal upperBand = sma20[index] + (2 * (decimal)stdDev20);
        decimal lowerBand = sma20[index] - (2 * (decimal)stdDev20);
        decimal bandWidth = upperBand - lowerBand;

        if (bandWidth == 0)
        {
            return 0.5f;
        }

        return (float)((ClosePrices[index] - lowerBand) / bandWidth);
    }

    private float CalculateVolumeChange(int index)
    {
        if (index < 1 || _volumes[index - 1] == 0)
        {
            return 0f;
        }

        return (float)((_volumes[index] - _volumes[index - 1]) / (double)_volumes[index - 1]);
    }

    private float CalculateVolumeMA(int index, int period)
    {
        if (index < period - 1)
        {
            return 0f;
        }

        long sum = 0;
        for (int j = 0; j < period; j++)
        {
            sum += _volumes[index - j];
        }
        return sum / (float)period;
    }

    private float CalculateVolumeRatio(int index)
    {
        float volumeMA = CalculateVolumeMA(index, 10);
        if (volumeMA == 0)
        {
            return 1f;
        }

        return _volumes[index] / volumeMA;
    }

    private float CalculatePriceVolumeCorrelation(int index, int period)
    {
        if (index < period)
        {
            return 0f;
        }

        var priceChanges = new List<double>();
        var volumeChanges = new List<double>();

        for (int i = index - period + 1; i <= index; i++)
        {
            if (i < 1)
            {
                continue;
            }

            if (ClosePrices[i - 1] != 0)
            {
                priceChanges.Add((double)((ClosePrices[i] - ClosePrices[i - 1]) / ClosePrices[i - 1]));
            }

            if (_volumes[i - 1] != 0)
            {
                volumeChanges.Add((_volumes[i] - _volumes[i - 1]) / (double)_volumes[i - 1]);
            }
        }

        if (priceChanges.Count < 2)
        {
            return 0f;
        }

        int n = priceChanges.Count;
        double sumX = priceChanges.Sum();
        double sumY = volumeChanges.Sum();
        double sumXY = priceChanges.Zip(volumeChanges, (a, b) => a * b).Sum();
        double sumX2 = priceChanges.Sum(a => a * a);
        double sumY2 = volumeChanges.Sum(b => b * b);

        double numerator = (n * sumXY) - (sumX * sumY);
        double denominator = Math.Sqrt(((n * sumX2) - (sumX * sumX)) * ((n * sumY2) - (sumY * sumY)));

        if (denominator == 0)
        {
            return 0f;
        }

        return (float)(numerator / denominator);
    }

    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            { "Algorithm", "FastTree Gradient Boosting (Walk-Forward)" },
            { "NumberOfLeaves", 31 },
            { "MinimumExampleCountPerLeaf", 20 },
            { "LearningRate", 0.1 },
            { "NumberOfTrees", 100 },
            { "BuyThreshold", _thresholds.BuyThreshold },
            { "SellThreshold", _thresholds.SellThreshold },
            { "MinTrainingBars", _minTrainingBars },
            { "ValidationMethod", "Walk-Forward" }
        };
    }
}

public record PricePrediction
{
    public float Score { get; set; }
}
