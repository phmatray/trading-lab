using Microsoft.Extensions.Options;
using Microsoft.ML;
using TradingStrat.Application.Configuration;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Common;
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

    public async Task<Result<LiveAnalysisResult>> ExecuteAsync(
        AnalysisCommand command,
        IProgress<string>? progress = null)
    {
        // Command validation happens in constructor - command is guaranteed to be valid here

        try
        {
            // Default to D1 (daily) if no timeframe specified
            TimeFrame timeFrame = command.TimeFrame ?? Domain.ValueObjects.TimeFrame.D1;

            progress?.Report("Loading historical data from database...");

            // Step 1: Load historical data from database
            List<HistoricalPrice> historicalData = await _historicalDataPort.GetHistoricalDataAsync(command.Ticker, timeFrame);

            if (historicalData.Count < 30)
            {
                return Result<LiveAnalysisResult>.Failure(
                    Error.InsufficientData(
                        $"Insufficient historical data. Required: 30+, Available: {historicalData.Count}. Please fetch historical data first.",
                        "INSUFFICIENT_HISTORICAL_DATA"));
            }

        List<HistoricalPrice> completeData;
        bool isFresh;
        string? warning;

        // Step 2: Fetch fresh data if requested
        if (command.FetchFreshData)
        {
            progress?.Report("Fetching latest market data...");
            (List<HistoricalPrice> freshData, isFresh, warning) = await FetchLatestDataAsync(command.Ticker, timeFrame);

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

        MarketFeatures[] features = featureEngine.BuildFeatureMatrix();

        progress?.Report("Training ML model...");

        // Step 4: Train ML model
        var mlContext = new MLContext(seed: 42);
        IDataView? trainingData = mlContext.Data.LoadFromEnumerable(features);

        var mlConfig = new MLModelConfiguration(
            NumberOfLeaves: _config.MachineLearning.ModelParameters.NumberOfLeaves,
            MinimumExampleCountPerLeaf: _config.MachineLearning.ModelParameters.MinimumExampleCountPerLeaf,
            LearningRate: _config.MachineLearning.ModelParameters.LearningRate,
            NumberOfTrees: _config.MachineLearning.ModelParameters.NumberOfTrees);

        ITransformer model = _mlModelPort.TrainModel(trainingData, mlConfig);

        progress?.Report("Generating prediction...");

        // Step 5: Generate prediction for next trading day
        int currentIndex = completeData.Count - 1;
        MarketFeatures currentFeatures = featureEngine.BuildFeaturesForIndex(currentIndex);

        float predictedReturn = _mlModelPort.Predict(model, currentFeatures);

        // Step 6: Build result object
        HistoricalPrice latestBar = completeData[currentIndex];
        HistoricalPrice previousBar = completeData[currentIndex - 1];

        // Step 7: Determine signal based on thresholds
        PredictionThresholds thresholds = command.Thresholds ?? new PredictionThresholds();
        TradeSignal signal = DetermineSignal(predictedReturn, thresholds, latestBar.Close ?? 0);

            progress?.Report("Analysis complete");

            return Result<LiveAnalysisResult>.Success(new LiveAnalysisResult
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
            });
        }
        catch (Exception ex)
        {
            return Result<LiveAnalysisResult>.Failure(
                Error.BusinessRule($"Failed to execute live analysis: {ex.Message}", "LIVE_ANALYSIS_FAILED"));
        }
    }

    private async Task<(List<HistoricalPrice> data, bool isFresh, string? warning)>
        FetchLatestDataAsync(string ticker, Domain.ValueObjects.TimeFrame timeFrame)
    {
        try
        {
            DateTime today = DateTime.Today;
            DateTime startDate = today.AddDays(-7);

            IReadOnlyList<HistoricalPrice> freshData = await _marketDataPort.FetchHistoricalDataAsync(ticker, timeFrame, startDate, today);

            if (freshData.Count == 0)
            {
                return ([], false,
                    "Yahoo Finance API returned no recent data. Using database data only.");
            }

            var freshDataList = freshData.ToList();
            DateTime latestFreshDate = freshDataList.Max(h => h.DateTime);
            int daysSinceLatest = (today - latestFreshDate).Days;

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
        decimal returnDecimal = (decimal)predictedReturn;
        float returnPercent = predictedReturn * 100;

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
