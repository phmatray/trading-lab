using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Infrastructure.MachineLearning;

public class MlNetModelAdapter : IMLModelPort
{
    private readonly MLContext _mlContext;
    private readonly ILogger<MlNetModelAdapter> _logger;

    public MlNetModelAdapter(ILogger<MlNetModelAdapter> logger)
    {
        _mlContext = new MLContext(seed: 42);
        _logger = logger;
    }

    public ITransformer TrainModel(IDataView trainingData, MLModelConfiguration config)
    {
        try
        {
            _logger.LogDebug("Starting ML model training with config: Leaves={Leaves}, MinExamples={MinExamples}, LearningRate={LearningRate}, Trees={Trees}",
                config.NumberOfLeaves, config.MinimumExampleCountPerLeaf, config.LearningRate, config.NumberOfTrees);

            int rowCount = (int)(trainingData.GetRowCount() ?? 0);
            _logger.LogDebug("Training data contains {RowCount} rows", rowCount);

            _logger.LogDebug("Training FastTree regression model...");
            DateTime startTime = DateTime.UtcNow;

            // Define the training pipeline
            EstimatorChain<RegressionPredictionTransformer<FastTreeRegressionModelParameters>>? pipeline = _mlContext.Transforms.Concatenate(
                    "Features",
                    // Price-based (5)
                    nameof(MarketFeatures.DailyReturn),
                    nameof(MarketFeatures.LogReturn),
                    nameof(MarketFeatures.HighLowRange),
                    nameof(MarketFeatures.OpenCloseRange),
                    nameof(MarketFeatures.PricePosition),
                    // Moving averages (6)
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
                    nameof(MarketFeatures.PriceVolumeCorrelation))
                .Append(_mlContext.Transforms.NormalizeMinMax("Features")) // Feature scaling
                .Append(_mlContext.Regression.Trainers.FastTree(
                    labelColumnName: nameof(MarketFeatures.NextDayReturn),
                    featureColumnName: "Features",
                    numberOfLeaves: config.NumberOfLeaves,
                    minimumExampleCountPerLeaf: config.MinimumExampleCountPerLeaf,
                    learningRate: config.LearningRate,
                    numberOfTrees: config.NumberOfTrees));

            // Train the model
            TransformerChain<RegressionPredictionTransformer<FastTreeRegressionModelParameters>>? model = pipeline.Fit(trainingData);

            TimeSpan elapsed = DateTime.UtcNow - startTime;
            _logger.LogDebug("Model training completed in {ElapsedMs}ms", elapsed.TotalMilliseconds);

            return model;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to train ML model");
            throw new InvalidOperationException($"Failed to train ML model: {ex.Message}", ex);
        }
    }

    public float Predict(ITransformer model, MarketFeatures features)
    {
        try
        {
            _logger.LogDebug("Making prediction for features");

            // Create prediction engine
            PredictionEngine<MarketFeatures, PricePrediction>? predictionEngine = _mlContext.Model.CreatePredictionEngine<MarketFeatures, PricePrediction>(model);

            // Make prediction
            PricePrediction? prediction = predictionEngine.Predict(features);

            _logger.LogDebug("Prediction generated: Score={Score}", prediction.Score);

            return prediction.Score;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to make prediction");
            throw new InvalidOperationException($"Failed to make prediction: {ex.Message}", ex);
        }
    }
}
