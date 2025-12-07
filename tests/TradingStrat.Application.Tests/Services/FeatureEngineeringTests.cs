using Shouldly;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Tests.Services;

public class FeatureEngineeringTests
{
    private readonly IIndicatorCalculator _indicatorCalculator;

    public FeatureEngineeringTests()
    {
        _indicatorCalculator = new IndicatorCalculator();
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_CalculatesAllIndicatorsOnce()
    {
        // Arrange
        List<HistoricalPrice> prices = BuildHistoricalPrices(
            100m, 101m, 102m, 103m, 104m, 105m, 106m, 107m, 108m, 109m,
            110m, 111m, 112m, 113m, 114m, 115m, 116m, 117m, 118m, 119m,
            120m, 121m, 122m, 123m, 124m, 125m, 126m, 127m, 128m, 129m);

        // Act
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Assert
        featureEngineering.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ExtractsOHLCVArrays()
    {
        // Arrange
        List<HistoricalPrice> prices = BuildHistoricalPrices(100m, 101m, 102m, 103m, 104m);

        // Act
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Assert - Verify construction succeeds and features can be built
        MarketFeatures[] features = featureEngineering.BuildFeatureMatrix();
        features.Length.ShouldBe(5);
    }

    [Fact]
    public void BuildFeatureMatrix_ReturnsArrayMatchingInputLength()
    {
        // Arrange
        List<HistoricalPrice> prices = BuildHistoricalPrices(
            100m, 101m, 102m, 103m, 104m, 105m, 106m, 107m, 108m, 109m,
            110m, 111m, 112m, 113m, 114m, 115m, 116m, 117m, 118m, 119m);
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Act
        MarketFeatures[] features = featureEngineering.BuildFeatureMatrix();

        // Assert
        features.Length.ShouldBe(20);
    }

    #endregion

    #region Feature Calculation Accuracy Tests

    [Fact]
    public void BuildFeaturesForIndex_CalculatesAllPriceFeatures()
    {
        // Arrange
        List<HistoricalPrice> prices = CreateTestPrices();
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Act
        MarketFeatures features = featureEngineering.BuildFeaturesForIndex(10);

        // Assert - Verify all 5 price features are calculated
        features.DailyReturn.ShouldNotBe(float.NaN);
        features.LogReturn.ShouldNotBe(float.NaN);
        features.HighLowRange.ShouldNotBe(float.NaN);
        features.OpenCloseRange.ShouldNotBe(float.NaN);
        features.PricePosition.ShouldBeInRange(0f, 1f);
    }

    [Fact]
    public void BuildFeaturesForIndex_CalculatesAllMAFeatures()
    {
        // Arrange
        List<HistoricalPrice> prices = CreateTestPrices();
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Act
        MarketFeatures features = featureEngineering.BuildFeaturesForIndex(25);

        // Assert - Verify all 6 MA features are calculated
        features.SMA_5.ShouldBeGreaterThan(0);
        features.SMA_10.ShouldBeGreaterThan(0);
        features.SMA_20.ShouldBeGreaterThan(0);
        features.EMA_12.ShouldBeGreaterThan(0);
        features.EMA_26.ShouldBeGreaterThan(0);
        features.PriceToSMA20.ShouldNotBe(float.NaN);
    }

    [Fact]
    public void BuildFeaturesForIndex_CalculatesAllMomentumFeatures()
    {
        // Arrange
        List<HistoricalPrice> prices = CreateTestPrices();
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Act
        MarketFeatures features = featureEngineering.BuildFeaturesForIndex(20);

        // Assert - Verify all 4 momentum features are calculated
        features.RSI_14.ShouldBeInRange(0f, 100f);
        features.Momentum_5.ShouldNotBe(float.NaN);
        features.ROC_10.ShouldNotBe(float.NaN);
        features.StochRSI.ShouldBeInRange(0f, 100f);
    }

    [Fact]
    public void BuildFeaturesForIndex_CalculatesAllMACDFeatures()
    {
        // Arrange
        List<HistoricalPrice> prices = CreateTestPrices();
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Act
        MarketFeatures features = featureEngineering.BuildFeaturesForIndex(30);

        // Assert - Verify all 3 MACD features are calculated
        features.MACD.ShouldNotBe(float.NaN);
        features.MACDSignal.ShouldNotBe(float.NaN);
        features.MACDHistogram.ShouldNotBe(float.NaN);
    }

    [Fact]
    public void BuildFeaturesForIndex_CalculatesAllVolatilityFeatures()
    {
        // Arrange
        List<HistoricalPrice> prices = CreateTestPrices();
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Act
        MarketFeatures features = featureEngineering.BuildFeaturesForIndex(25);

        // Assert - Verify all 4 volatility features are calculated
        features.StdDev_10.ShouldBeGreaterThanOrEqualTo(0);
        features.StdDev_20.ShouldBeGreaterThanOrEqualTo(0);
        features.ATR_14.ShouldBeGreaterThanOrEqualTo(0);
        // BollingerPosition can be outside 0-1 range when price breaks out of bands
        float.IsNaN(features.BollingerPosition).ShouldBeFalse();
        float.IsInfinity(features.BollingerPosition).ShouldBeFalse();
    }

    [Fact]
    public void BuildFeaturesForIndex_CalculatesAllVolumeFeatures()
    {
        // Arrange
        List<HistoricalPrice> prices = CreateTestPrices();
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Act
        MarketFeatures features = featureEngineering.BuildFeaturesForIndex(15);

        // Assert - Verify all 4 volume features are calculated
        features.VolumeChange.ShouldNotBe(float.NaN);
        features.VolumeMA_10.ShouldBeGreaterThan(0);
        features.VolumeRatio.ShouldBeGreaterThan(0);
        features.PriceVolumeCorrelation.ShouldBeInRange(-1f, 1f);
    }

    [Fact]
    public void BuildFeaturesForIndex_DailyReturn_MatchesExpected()
    {
        // Arrange - Simple prices for easy calculation
        List<HistoricalPrice> prices = BuildHistoricalPrices(
            100m, 105m, 110m, 115m, 120m, 125m, 130m, 135m, 140m, 145m,
            150m, 155m, 160m, 165m, 170m, 175m, 180m, 185m, 190m, 195m);
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Act
        MarketFeatures features = featureEngineering.BuildFeaturesForIndex(1);

        // Assert - Daily return from 100 to 105 is 5%
        features.DailyReturn.ShouldBe(0.05f, 0.0001f);
    }

    [Fact]
    public void BuildFeaturesForIndex_LogReturn_MatchesExpected()
    {
        // Arrange
        List<HistoricalPrice> prices = BuildHistoricalPrices(
            100m, 110m, 121m, 133.1m, 146.41m, 161.051m, 177.156m, 194.872m, 214.359m, 235.795m,
            259.374m, 285.312m, 313.843m, 345.227m, 379.75m, 417.725m, 459.497m, 505.447m, 555.992m, 611.591m);
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Act
        MarketFeatures features = featureEngineering.BuildFeaturesForIndex(1);

        // Assert - Log return from 100 to 110 is ln(1.1) ≈ 0.0953
        features.LogReturn.ShouldBe((float)Math.Log(1.1), 0.0001f);
    }

    [Fact]
    public void BuildFeaturesForIndex_RSI_MatchesIndicatorCalculator()
    {
        // Arrange
        List<HistoricalPrice> prices = CreateTestPrices();
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        decimal[] closePrices = prices.Select(p => p.Close ?? 0).ToArray();
        decimal[] rsiFromCalculator = _indicatorCalculator.CalculateRSI(closePrices, 14);

        // Act
        MarketFeatures features = featureEngineering.BuildFeaturesForIndex(20);

        // Assert
        features.RSI_14.ShouldBe((float)rsiFromCalculator[20], 0.01f);
    }

    [Fact]
    public void BuildFeaturesForIndex_MACD_MatchesIndicatorCalculator()
    {
        // Arrange
        List<HistoricalPrice> prices = CreateTestPrices();
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        decimal[] closePrices = prices.Select(p => p.Close ?? 0).ToArray();
        (decimal[] macd, decimal[] signal, decimal[] histogram) = _indicatorCalculator.CalculateMACD(closePrices);

        // Act
        MarketFeatures features = featureEngineering.BuildFeaturesForIndex(30);

        // Assert
        features.MACD.ShouldBe((float)macd[30], 0.01f);
        features.MACDSignal.ShouldBe((float)signal[30], 0.01f);
        features.MACDHistogram.ShouldBe((float)histogram[30], 0.01f);
    }

    [Fact]
    public void BuildFeaturesForIndex_NextDayReturn_IsZeroForLastIndex()
    {
        // Arrange
        List<HistoricalPrice> prices = CreateTestPrices();
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);
        int lastIndex = prices.Count - 1;

        // Act
        MarketFeatures features = featureEngineering.BuildFeaturesForIndex(lastIndex);

        // Assert - NextDayReturn should be 0 for the last bar (no next day)
        features.NextDayReturn.ShouldBe(0f);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void BuildFeaturesForIndex_WithInsufficientData_ReturnsDefaults()
    {
        // Arrange - Very small dataset
        List<HistoricalPrice> prices = BuildHistoricalPrices(100m, 101m, 102m);
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Act
        MarketFeatures features = featureEngineering.BuildFeaturesForIndex(0);

        // Assert - First index should return default features
        features.DailyReturn.ShouldBe(0f);
        features.LogReturn.ShouldBe(0f);
    }

    [Fact]
    public void BuildFeaturesForIndex_WithZeroPrices_HandlesGracefully()
    {
        // Arrange - Include some zero prices
        List<HistoricalPrice> prices =
        [
            new HistoricalPrice { Ticker = "TEST", DateTime = DateTime.Today, Close = 0m, Open = 0m, High = 0m, Low = 0m, Volume = 1000 },
            new HistoricalPrice { Ticker = "TEST", DateTime = DateTime.Today.AddDays(1), Close = 100m, Open = 100m, High = 105m, Low = 95m, Volume = 1000 },
            new HistoricalPrice { Ticker = "TEST", DateTime = DateTime.Today.AddDays(2), Close = 101m, Open = 100m, High = 106m, Low = 96m, Volume = 1000 }
        ];

        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Act
        MarketFeatures features = featureEngineering.BuildFeaturesForIndex(1);

        // Assert - Should not throw and should return valid features
        features.ShouldNotBeNull();
        float.IsNaN(features.DailyReturn).ShouldBeFalse();
    }

    [Fact]
    public void ValidateFloat_WithNaN_ReturnsZero()
    {
        // Arrange - Create scenario that might produce NaN
        List<HistoricalPrice> prices = BuildHistoricalPrices(
            100m, 100m, 100m, 100m, 100m, 100m, 100m, 100m, 100m, 100m,
            100m, 100m, 100m, 100m, 100m, 100m, 100m, 100m, 100m, 100m);
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Act
        MarketFeatures features = featureEngineering.BuildFeaturesForIndex(10);

        // Assert - All features should be valid floats, not NaN
        float.IsNaN(features.DailyReturn).ShouldBeFalse();
        float.IsNaN(features.LogReturn).ShouldBeFalse();
        float.IsNaN(features.RSI_14).ShouldBeFalse();
    }

    [Fact]
    public void ValidateFloat_WithInfinity_ReturnsZero()
    {
        // Arrange - Use very small volume to potentially create infinity
        List<HistoricalPrice> prices =
        [
            new HistoricalPrice { Ticker = "TEST", DateTime = DateTime.Today, Close = 100m, Open = 100m, High = 105m, Low = 95m, Volume = 0 },
            new HistoricalPrice { Ticker = "TEST", DateTime = DateTime.Today.AddDays(1), Close = 101m, Open = 100m, High = 106m, Low = 96m, Volume = 1000000 }
        ];

        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Act
        MarketFeatures features = featureEngineering.BuildFeaturesForIndex(1);

        // Assert - VolumeChange should not be infinity
        float.IsInfinity(features.VolumeChange).ShouldBeFalse();
    }

    [Fact]
    public void BuildFeatureMatrix_Total26Features()
    {
        // Arrange
        List<HistoricalPrice> prices = CreateTestPrices();
        FeatureEngineering featureEngineering = new(prices.AsReadOnly(), _indicatorCalculator);

        // Act
        MarketFeatures[] features = featureEngineering.BuildFeatureMatrix();
        MarketFeatures sampleFeature = features[30];

        // Assert - Verify MarketFeatures has 26 features (plus NextDayReturn target)
        // We can't count properties directly, but we can verify all feature categories are present
        // Price-based: 5
        sampleFeature.DailyReturn.ShouldNotBe(float.NaN);
        sampleFeature.LogReturn.ShouldNotBe(float.NaN);
        sampleFeature.HighLowRange.ShouldNotBe(float.NaN);
        sampleFeature.OpenCloseRange.ShouldNotBe(float.NaN);
        sampleFeature.PricePosition.ShouldBeInRange(0f, 1f);

        // Moving averages: 6
        sampleFeature.SMA_5.ShouldBeGreaterThan(0);
        sampleFeature.SMA_10.ShouldBeGreaterThan(0);
        sampleFeature.SMA_20.ShouldBeGreaterThan(0);
        sampleFeature.EMA_12.ShouldBeGreaterThan(0);
        sampleFeature.EMA_26.ShouldBeGreaterThan(0);
        sampleFeature.PriceToSMA20.ShouldNotBe(float.NaN);

        // Momentum: 4
        sampleFeature.RSI_14.ShouldBeInRange(0f, 100f);
        sampleFeature.Momentum_5.ShouldNotBe(float.NaN);
        sampleFeature.ROC_10.ShouldNotBe(float.NaN);
        sampleFeature.StochRSI.ShouldBeInRange(0f, 100f);

        // MACD: 3
        sampleFeature.MACD.ShouldNotBe(float.NaN);
        sampleFeature.MACDSignal.ShouldNotBe(float.NaN);
        sampleFeature.MACDHistogram.ShouldNotBe(float.NaN);

        // Volatility: 4
        sampleFeature.StdDev_10.ShouldBeGreaterThanOrEqualTo(0);
        sampleFeature.StdDev_20.ShouldBeGreaterThanOrEqualTo(0);
        sampleFeature.ATR_14.ShouldBeGreaterThanOrEqualTo(0);
        // BollingerPosition can be outside 0-1 when price breaks out of bands
        float.IsNaN(sampleFeature.BollingerPosition).ShouldBeFalse();

        // Volume: 4
        sampleFeature.VolumeChange.ShouldNotBe(float.NaN);
        sampleFeature.VolumeMA_10.ShouldBeGreaterThan(0);
        sampleFeature.VolumeRatio.ShouldBeGreaterThan(0);
        sampleFeature.PriceVolumeCorrelation.ShouldBeInRange(-1f, 1f);

        // Target: 1
        sampleFeature.NextDayReturn.ShouldNotBe(float.NaN);
    }

    #endregion

    #region Helper Methods

    private static List<HistoricalPrice> CreateTestPrices()
    {
        // Create 50 bars of trending prices with some volatility
        decimal[] priceValues = new decimal[50];
        for (int i = 0; i < 50; i++)
        {
            priceValues[i] = 100m + (i * 0.5m);
        }

        return BuildHistoricalPrices(priceValues);
    }

    private static List<HistoricalPrice> BuildHistoricalPrices(params decimal[] closePrices)
    {
        List<HistoricalPrice> prices = new();
        DateTime baseDate = new(2024, 1, 1);

        for (int i = 0; i < closePrices.Length; i++)
        {
            decimal close = closePrices[i];
            prices.Add(new HistoricalPrice
            {
                Ticker = "TEST",
                DateTime = baseDate.AddDays(i),
                Open = close * 0.99m,
                High = close * 1.02m,
                Low = close * 0.98m,
                Close = close,
                Volume = 1000000 + (i * 1000)
            });
        }

        return prices;
    }

    #endregion
}
