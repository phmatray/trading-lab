using Microsoft.ML;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Outbound port for machine learning model operations using ML.NET.
/// Separates ML training and prediction concerns from domain strategies.
/// This eliminates the reflection hack that was used in the original architecture
/// to access MachineLearningStrategy's private _featureEngine field.
/// </summary>
public interface IMLModelPort
{
    /// <summary>
    /// Trains a FastTree gradient boosting regression model on market features.
    /// </summary>
    /// <param name="trainingData">ML.NET DataView containing 26 technical indicators as features.</param>
    /// <param name="config">Model hyperparameters for FastTree algorithm.</param>
    /// <returns>Trained ML.NET transformer model ready for predictions.</returns>
    ITransformer TrainModel(IDataView trainingData, MLModelConfiguration config);

    /// <summary>
    /// Predicts next-day return using a trained model and current market features.
    /// </summary>
    /// <param name="model">Trained ML.NET transformer model.</param>
    /// <param name="features">Market features for the current time point (26 technical indicators).</param>
    /// <returns>Predicted next-day return as a decimal value.</returns>
    float Predict(ITransformer model, MarketFeatures features);

    /// <summary>
    /// Converts market features array to ML.NET IDataView format for training.
    /// </summary>
    /// <param name="features">Array of market features with technical indicators.</param>
    /// <returns>ML.NET DataView ready for model training.</returns>
    IDataView CreateDataView(MarketFeatures[] features);
}

/// <summary>
/// Configuration for ML.NET FastTree gradient boosting model.
/// </summary>
/// <param name="NumberOfLeaves">Maximum number of leaves per decision tree (default 31).</param>
/// <param name="MinimumExampleCountPerLeaf">Minimum samples required in each leaf node (default 20).</param>
/// <param name="LearningRate">Learning rate for gradient boosting (default 0.1).</param>
/// <param name="NumberOfTrees">Number of boosting iterations/trees to create (default 100).</param>
public record MLModelConfiguration(
    int NumberOfLeaves = 31,
    int MinimumExampleCountPerLeaf = 20,
    double LearningRate = 0.1,
    int NumberOfTrees = 100);
