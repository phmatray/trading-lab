using Microsoft.ML;
using Microsoft.ML.Data;
using TradingStrat.Models;

namespace TradingStrat.Services.Strategies.MachineLearning;

public class MachineLearningStrategy : BaseStrategy
{
    private readonly PredictionThresholds _thresholds;
    private readonly int _minTrainingBars;

    private MLContext _mlContext = null!;
    private ITransformer _trainedModel = null!;
    private PredictionEngine<MarketFeatures, PricePrediction> _predictionEngine = null!;
    private MarketFeatures[] _featureMatrix = null!;
    private FeatureEngineering _featureEngine = null!;

    public override string Name => "ML FastTree Regression";

    public override string Description =>
        $"Machine learning strategy using FastTree gradient boosting to predict next-day price changes. " +
        $"Buy threshold: {_thresholds.BuyThreshold:P2}, Sell threshold: {_thresholds.SellThreshold:P2}";

    public MachineLearningStrategy(
        PredictionThresholds? thresholds = null,
        int minTrainingBars = 100)
    {
        _thresholds = thresholds ?? new PredictionThresholds();
        _minTrainingBars = minTrainingBars;
    }

    public override void Initialize(IReadOnlyList<HistoricalPrice> historicalData)
    {
        base.Initialize(historicalData);

        if (historicalData.Count < _minTrainingBars)
        {
            throw new InvalidOperationException(
                $"Insufficient data for training. Required: {_minTrainingBars}, Available: {historicalData.Count}");
        }

        // Step 1: Feature Engineering
        _featureEngine = new FeatureEngineering(historicalData, ClosePrices, this);
        _featureMatrix = _featureEngine.BuildFeatureMatrix();

        // Step 2: Prepare Training Data
        var trainingData = PrepareTrainingData();

        // Step 3: Train Model
        TrainModel(trainingData);

        // Step 4: Create Prediction Engine
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<MarketFeatures, PricePrediction>(_trainedModel);
    }

    private IDataView PrepareTrainingData()
    {
        _mlContext = new MLContext(seed: 42);

        // Exclude last bar (no target) and first N bars (incomplete indicators)
        var validStartIndex = 30; // After longest indicator period
        var trainingFeatures = _featureMatrix
            .Skip(validStartIndex)
            .Take(_featureMatrix.Length - validStartIndex - 1) // Exclude last bar
            .ToList();

        return _mlContext.Data.LoadFromEnumerable(trainingFeatures);
    }

    private void TrainModel(IDataView trainingData)
    {
        // Define all feature column names (26 features)
        var featureColumns = new[]
        {
            // Price-based (5)
            nameof(MarketFeatures.DailyReturn),
            nameof(MarketFeatures.LogReturn),
            nameof(MarketFeatures.HighLowRange),
            nameof(MarketFeatures.OpenCloseRange),
            nameof(MarketFeatures.PricePosition),
            // Moving Averages (6)
            nameof(MarketFeatures.SMA_5),
            nameof(MarketFeatures.SMA_10),
            nameof(MarketFeatures.SMA_20),
            nameof(MarketFeatures.EMA_12),
            nameof(MarketFeatures.EMA_26),
            nameof(MarketFeatures.PriceToSMA20),
            // Momentum (4)
            nameof(MarketFeatures.RSI_14),
            nameof(MarketFeatures.Momentum_5),
            nameof(MarketFeatures.ROC_10),
            nameof(MarketFeatures.StochRSI),
            // MACD (3)
            nameof(MarketFeatures.MACD),
            nameof(MarketFeatures.MACDSignal),
            nameof(MarketFeatures.MACDHistogram),
            // Volatility (4)
            nameof(MarketFeatures.StdDev_10),
            nameof(MarketFeatures.StdDev_20),
            nameof(MarketFeatures.ATR_14),
            nameof(MarketFeatures.BollingerPosition),
            // Volume (4)
            nameof(MarketFeatures.VolumeChange),
            nameof(MarketFeatures.VolumeMA_10),
            nameof(MarketFeatures.VolumeRatio),
            nameof(MarketFeatures.PriceVolumeCorrelation)
        };

        // Build ML Pipeline
        var pipeline = _mlContext.Transforms
            .Concatenate("Features", featureColumns)
            .Append(_mlContext.Transforms.NormalizeMinMax("Features")) // Feature scaling
            .Append(_mlContext.Regression.Trainers.FastTree(
                labelColumnName: nameof(MarketFeatures.NextDayReturn),
                featureColumnName: "Features",
                numberOfLeaves: 31,
                minimumExampleCountPerLeaf: 20,
                learningRate: 0.1,
                numberOfTrees: 100));

        // Train the model
        try
        {
            _trainedModel = pipeline.Fit(trainingData);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Model training failed", ex);
        }
    }

    public override TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition)
    {
        // Check if we have enough data
        if (currentIndex < 30)
        {
            return new TradeSignal(SignalType.Hold, 0, 0, "Insufficient data for ML prediction");
        }

        var currentPrice = ClosePrices[currentIndex];

        // Extract features for current bar (using only data up to currentIndex)
        var currentFeatures = _featureMatrix[currentIndex];

        // Predict next day's return
        float predictedReturn;
        try
        {
            var prediction = _predictionEngine.Predict(currentFeatures);
            predictedReturn = prediction.Score;
        }
        catch (Exception ex)
        {
            return new TradeSignal(SignalType.Hold, 0, 0, $"Prediction error: {ex.Message}");
        }

        // Convert prediction to signal
        var signalType = _thresholds.ConvertPredictionToSignal(predictedReturn);

        // Generate trading signal based on prediction and position
        switch (signalType)
        {
            case SignalType.Buy when currentPosition == 0:
                var quantity = CalculateQuantity(currentCash, currentPrice, currentPosition);
                if (quantity > 0)
                {
                    return new TradeSignal(
                        SignalType.Buy,
                        currentPrice,
                        quantity,
                        $"ML predicts +{predictedReturn:P2} return");
                }
                break;

            case SignalType.Sell when currentPosition > 0:
                return new TradeSignal(
                    SignalType.Sell,
                    currentPrice,
                    currentPosition,
                    $"ML predicts {predictedReturn:P2} return");
        }

        return new TradeSignal(SignalType.Hold, 0, 0, $"ML predicts {predictedReturn:P2}, holding");
    }

    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            { "Algorithm", "FastTree Gradient Boosting" },
            { "Features", 26 },
            { "BuyThreshold", _thresholds.BuyThreshold },
            { "SellThreshold", _thresholds.SellThreshold },
            { "MinTrainingBars", _minTrainingBars }
        };
    }
}
