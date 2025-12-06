using System.Text.RegularExpressions;
using TradingStrat.Data;
using TradingStrat.Models;
using TradingStrat.Services.Strategies.MachineLearning;

namespace TradingStrat.Services.LiveAnalysis;

public class LiveAnalysisEngine : ILiveAnalysisEngine
{
    private readonly IDataRepository _repository;
    private readonly IYahooFinanceService _yahooService;

    public LiveAnalysisEngine(IDataRepository repository, IYahooFinanceService yahooService)
    {
        _repository = repository;
        _yahooService = yahooService;
    }

    public async Task<LiveAnalysisResult> AnalyzeCurrentPositionAsync(
        string ticker,
        PredictionThresholds? thresholds = null,
        IProgress<string>? progress = null)
    {
        progress?.Report("Loading historical data from database...");

        // Step 1: Load historical data from database
        var historicalData = await _repository.GetHistoricalDataAsync(ticker);

        if (historicalData.Count < 30)
        {
            throw new InvalidOperationException(
                $"Insufficient historical data. Required: 30+, Available: {historicalData.Count}. " +
                "Please fetch historical data first.");
        }

        progress?.Report("Fetching latest market data...");

        // Step 2: Fetch fresh data from Yahoo API
        var (freshData, isFresh, warning) = await FetchLatestDataAsync(ticker, historicalData);

        progress?.Report("Merging and preparing data...");

        // Step 3: Merge historical + fresh data
        var completeData = MergeHistoricalData(historicalData, freshData);

        progress?.Report("Training ML model...");

        // Step 4: Train ML model on all available data
        var strategy = new MachineLearningStrategy(
            thresholds ?? new PredictionThresholds(),
            minTrainingBars: 30);

        strategy.Initialize(completeData);

        progress?.Report("Generating prediction...");

        // Step 5: Generate prediction for next trading day
        var currentIndex = completeData.Count - 1;
        var signal = strategy.GenerateSignal(currentIndex, 10_000m, 0);

        progress?.Report("Extracting features...");

        // Step 6: Extract feature values for current bar
        var featureEngine = GetFeatureEngine(strategy);
        var currentFeatures = featureEngine.BuildFeaturesForIndex(currentIndex);

        // Step 7: Build result object
        var latestBar = completeData[currentIndex];
        var previousBar = completeData[currentIndex - 1];

        return new LiveAnalysisResult
        {
            Ticker = ticker,
            AnalysisDateTime = DateTime.Now,
            LatestDataDate = latestBar.DateTime,
            CurrentPrice = latestBar.Close ?? 0,
            PreviousClose = previousBar.Close ?? 0,
            DailyChange = (latestBar.Close ?? 0) - (previousBar.Close ?? 0),
            DailyChangePercent = ((latestBar.Close ?? 0) - (previousBar.Close ?? 0))
                               / (previousBar.Close ?? 1) * 100,
            PredictedSignal = signal.Type,
            PredictedReturn = ExtractPredictedReturn(signal.Reason),
            PredictionReason = signal.Reason,
            CurrentFeatures = currentFeatures,
            TrainingDataPoints = completeData.Count,
            OldestTrainingDate = completeData[0].DateTime,
            IsDataFresh = isFresh,
            DataFreshnessWarning = warning
        };
    }

    private async Task<(List<HistoricalPrice> data, bool isFresh, string? warning)>
        FetchLatestDataAsync(string ticker, List<HistoricalPrice> historicalData)
    {
        try
        {
            var latestDbDate = historicalData.Max(h => h.DateTime);
            var today = DateTime.Today;

            // Fetch last 7 days to ensure we have fresh data
            var startDate = today.AddDays(-7);
            var freshDataPoints = await _yahooService.GetHistoricalDataAsync(
                ticker, startDate, today);

            if (freshDataPoints.Count == 0)
            {
                return (new List<HistoricalPrice>(), false,
                    "Yahoo Finance API returned no recent data. Using database data only.");
            }

            // Convert to HistoricalPrice entities
            var freshData = freshDataPoints
                .Select(dp => new HistoricalPrice
                {
                    Ticker = ticker,
                    DateTime = dp.DateTime,
                    Open = dp.Open,
                    High = dp.High,
                    Low = dp.Low,
                    Close = dp.Close,
                    AdjustedClose = dp.AdjustedClose,
                    Volume = dp.Volume
                })
                .ToList();

            var latestFreshDate = freshData.Max(h => h.DateTime);
            var daysSinceLatest = (today - latestFreshDate).Days;

            if (daysSinceLatest > 3)
            {
                return (freshData, false,
                    $"Latest data is {daysSinceLatest} days old. Market may be closed.");
            }

            return (freshData, true, null);
        }
        catch (Exception ex)
        {
            return (new List<HistoricalPrice>(), false,
                $"Failed to fetch fresh data: {ex.Message}. Using database data only.");
        }
    }

    private List<HistoricalPrice> MergeHistoricalData(
        List<HistoricalPrice> historicalData,
        List<HistoricalPrice> freshData)
    {
        // Create dictionary of existing dates
        var existingDates = new HashSet<DateTime>(
            historicalData.Select(h => h.DateTime.Date));

        // Add fresh data that doesn't exist
        var newData = freshData
            .Where(f => !existingDates.Contains(f.DateTime.Date))
            .ToList();

        // Combine and sort
        var combined = historicalData.Concat(newData)
            .OrderBy(h => h.DateTime)
            .ToList();

        return combined;
    }

    private FeatureEngineering GetFeatureEngine(MachineLearningStrategy strategy)
    {
        // Use reflection to access private _featureEngine field
        var field = typeof(MachineLearningStrategy)
            .GetField("_featureEngine",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        return (FeatureEngineering)field!.GetValue(strategy)!;
    }

    private float ExtractPredictedReturn(string reason)
    {
        // Parse "ML predicts +1.23% return" or "ML predicts -1.23%" format
        var match = Regex.Match(reason, @"ML predicts ([+-]?\d+\.?\d*)%");

        if (match.Success && float.TryParse(match.Groups[1].Value, out var value))
        {
            return value / 100; // Convert percentage to decimal
        }

        return 0f;
    }
}
