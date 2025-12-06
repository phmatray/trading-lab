using Microsoft.Extensions.Options;
using Microsoft.ML;
using TradingStrat.Application.Configuration;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

public class AnalyzeCurrentPositionUseCase : ILiveAnalysisUseCase
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly IMarketDataPort _marketDataPort;
    private readonly IMLModelPort _mlModelPort;
    private readonly IIndicatorCalculator _indicatorCalculator;
    private readonly TradingConfiguration _config;

    public AnalyzeCurrentPositionUseCase(
        IHistoricalDataPort historicalDataPort,
        IMarketDataPort marketDataPort,
        IMLModelPort mlModelPort,
        IIndicatorCalculator indicatorCalculator,
        IOptions<TradingConfiguration> config)
    {
        _historicalDataPort = historicalDataPort;
        _marketDataPort = marketDataPort;
        _mlModelPort = mlModelPort;
        _indicatorCalculator = indicatorCalculator;
        _config = config.Value;
    }

    public async Task<LiveAnalysisResult> ExecuteAsync(
        AnalysisCommand command,
        IProgress<string>? progress = null)
    {
        progress?.Report("Loading historical data from database...");

        // Step 1: Load historical data from database
        var historicalData = await _historicalDataPort.GetHistoricalDataAsync(command.Ticker);

        if (historicalData.Count < 30)
        {
            throw new InvalidOperationException(
                $"Insufficient historical data. Required: 30+, Available: {historicalData.Count}. " +
                "Please fetch historical data first.");
        }

        List<HistoricalPrice> completeData;
        bool isFresh;
        string? warning;

        // Step 2: Fetch fresh data if requested
        if (command.FetchFreshData)
        {
            progress?.Report("Fetching latest market data...");
            (var freshData, isFresh, warning) = await FetchLatestDataAsync(command.Ticker);

            progress?.Report("Merging and preparing data...");
            completeData = MergeHistoricalData(historicalData, freshData);
        }
        else
        {
            completeData = historicalData;
            isFresh = false;
            warning = "Using database data only (fresh data fetch disabled)";
        }

        progress?.Report("Calculating technical indicators...");

        // Step 3: Calculate features using FeatureEngineering
        var featureEngine = new FeatureEngineering(
            completeData.AsReadOnly(),
            _indicatorCalculator);

        var features = featureEngine.BuildFeatureMatrix();

        progress?.Report("Training ML model...");

        // Step 4: Train ML model
        var mlContext = new MLContext(seed: 42);
        var trainingData = mlContext.Data.LoadFromEnumerable(features);

        var mlConfig = new MLModelConfiguration(
            NumberOfLeaves: _config.MachineLearning.ModelParameters.NumberOfLeaves,
            MinimumExampleCountPerLeaf: _config.MachineLearning.ModelParameters.MinimumExampleCountPerLeaf,
            LearningRate: _config.MachineLearning.ModelParameters.LearningRate,
            NumberOfTrees: _config.MachineLearning.ModelParameters.NumberOfTrees);

        var model = _mlModelPort.TrainModel(trainingData, mlConfig);

        progress?.Report("Generating prediction...");

        // Step 5: Generate prediction for next trading day
        var currentIndex = completeData.Count - 1;
        var currentFeatures = featureEngine.BuildFeaturesForIndex(currentIndex);

        var predictedReturn = _mlModelPort.Predict(model, currentFeatures);

        // Step 6: Build result object
        var latestBar = completeData[currentIndex];
        var previousBar = completeData[currentIndex - 1];

        // Step 7: Determine signal based on thresholds
        var thresholds = command.Thresholds ?? new PredictionThresholds();
        var signal = DetermineSignal(predictedReturn, thresholds, latestBar.Close ?? 0);

        progress?.Report("Analysis complete");

        return new LiveAnalysisResult
        {
            Ticker = command.Ticker,
            AnalysisDateTime = DateTime.Now,
            LatestDataDate = latestBar.DateTime,
            CurrentPrice = latestBar.Close ?? 0,
            PreviousClose = previousBar.Close ?? 0,
            DailyChange = (latestBar.Close ?? 0) - (previousBar.Close ?? 0),
            DailyChangePercent = ((latestBar.Close ?? 0) - (previousBar.Close ?? 0))
                               / (previousBar.Close ?? 1) * 100,
            PredictedSignal = signal.Type,
            PredictedReturn = predictedReturn,
            PredictionReason = signal.Reason,
            CurrentFeatures = currentFeatures,
            TrainingDataPoints = completeData.Count,
            OldestTrainingDate = completeData[0].DateTime,
            IsDataFresh = isFresh,
            DataFreshnessWarning = warning
        };
    }

    private async Task<(List<HistoricalPrice> data, bool isFresh, string? warning)>
        FetchLatestDataAsync(string ticker)
    {
        try
        {
            var today = DateTime.Today;
            var startDate = today.AddDays(-7);

            var freshData = await _marketDataPort.FetchHistoricalDataAsync(ticker, startDate, today);

            if (freshData.Count == 0)
            {
                return ([], false,
                    "Yahoo Finance API returned no recent data. Using database data only.");
            }

            var freshDataList = freshData.ToList();
            var latestFreshDate = freshDataList.Max(h => h.DateTime);
            var daysSinceLatest = (today - latestFreshDate).Days;

            if (daysSinceLatest > 3)
            {
                return (freshDataList, false,
                    $"Latest data is {daysSinceLatest} days old. Market may be closed.");
            }

            return (freshDataList, true, null);
        }
        catch (Exception ex)
        {
            return ([], false,
                $"Failed to fetch fresh data: {ex.Message}. Using database data only.");
        }
    }

    private List<HistoricalPrice> MergeHistoricalData(
        List<HistoricalPrice> historicalData,
        List<HistoricalPrice> freshData)
    {
        var existingDates = new HashSet<DateTime>(
            historicalData.Select(h => h.DateTime.Date));

        var newData = freshData
            .Where(f => !existingDates.Contains(f.DateTime.Date))
            .ToList();

        var combined = historicalData.Concat(newData)
            .OrderBy(h => h.DateTime)
            .ToList();

        return combined;
    }

    private TradeSignal DetermineSignal(float predictedReturn, PredictionThresholds thresholds, decimal currentPrice)
    {
        var returnDecimal = (decimal)predictedReturn;
        var returnPercent = predictedReturn * 100;

        if (returnDecimal >= thresholds.BuyThreshold)
        {
            return new TradeSignal(
                SignalType.Buy,
                currentPrice,
                0, // Quantity not applicable for prediction signal
                $"ML predicts +{returnPercent:F2}% return (threshold: {thresholds.BuyThreshold * 100:F2}%)");
        }

        if (returnDecimal <= thresholds.SellThreshold)
        {
            return new TradeSignal(
                SignalType.Sell,
                currentPrice,
                0,
                $"ML predicts {returnPercent:F2}% return (threshold: {thresholds.SellThreshold * 100:F2}%)");
        }

        return new TradeSignal(
            SignalType.Hold,
            currentPrice,
            0,
            $"ML predicts {returnPercent:F2}% return (within thresholds)");
    }
}
