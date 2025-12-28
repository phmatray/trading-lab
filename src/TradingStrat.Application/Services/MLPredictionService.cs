using Microsoft.Extensions.Logging;
using Microsoft.ML;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Services;

/// <summary>
/// Implementation of IMLPredictionService that delegates ML operations to the Infrastructure layer via IMLModelPort.
/// This service bridges the Domain layer (which requires IMLPredictionService) and the Infrastructure layer (which provides IMLModelPort).
/// </summary>
public class MLPredictionService : IMLPredictionService
{
    private readonly IMLModelPort _mlModelPort;
    private readonly ILogger<MLPredictionService> _logger;
    private ITransformer? _currentModel;
    private readonly MLModelConfiguration _defaultConfig;

    public MLPredictionService(
        IMLModelPort mlModelPort,
        ILogger<MLPredictionService> logger)
    {
        _mlModelPort = mlModelPort ?? throw new ArgumentNullException(nameof(mlModelPort));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Default FastTree configuration (matches CLAUDE.md specifications)
        _defaultConfig = new MLModelConfiguration(
            NumberOfTrees: 100,
            LearningRate: 0.1,
            NumberOfLeaves: 31,
            MinimumExampleCountPerLeaf: 20
        );
    }

    /// <inheritdoc />
    public void Train(MarketFeatures[] features, int currentIndex)
    {
        if (features is null || features.Length == 0)
        {
            throw new ArgumentException("Features array cannot be null or empty", nameof(features));
        }

        if (currentIndex < 0 || currentIndex >= features.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(currentIndex),
                "Current index must be within the bounds of the features array");
        }

        _logger.LogDebug("Training ML model with {FeatureCount} features up to index {CurrentIndex}",
            features.Length, currentIndex);

        try
        {
            // Only use features up to currentIndex for walk-forward validation (avoid look-ahead bias)
            MarketFeatures[] trainingFeatures = features.Take(currentIndex + 1).ToArray();

            // Convert to IDataView for ML.NET
            IDataView trainingData = _mlModelPort.CreateDataView(trainingFeatures);

            // Train model using Infrastructure layer
            _currentModel = _mlModelPort.TrainModel(trainingData, _defaultConfig);

            _logger.LogDebug("ML model training completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to train ML model");
            throw new InvalidOperationException("Failed to train ML prediction model", ex);
        }
    }

    /// <inheritdoc />
    public decimal Predict(MarketFeatures features)
    {
        if (features is null)
        {
            throw new ArgumentNullException(nameof(features));
        }

        if (_currentModel is null)
        {
            throw new InvalidOperationException("Model has not been trained yet. Call Train() before Predict().");
        }

        try
        {
            // Make prediction using Infrastructure layer
            float prediction = _mlModelPort.Predict(_currentModel, features);

            // Convert float to decimal (ML.NET uses float for predictions)
            return (decimal)prediction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to make prediction");
            throw new InvalidOperationException("Failed to make ML prediction", ex);
        }
    }
}
