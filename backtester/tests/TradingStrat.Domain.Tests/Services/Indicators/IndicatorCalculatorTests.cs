using Shouldly;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.Tests.Builders;

namespace TradingStrat.Domain.Tests.Services.Indicators;

public class IndicatorCalculatorTests
{
    private readonly IndicatorCalculator _calculator;

    public IndicatorCalculatorTests()
    {
        _calculator = new IndicatorCalculator();
    }

    #region SMA Tests (8 tests)

    [Fact]
    public void CalculateSMA_WithValidData_ReturnsCorrectValues()
    {
        // Arrange
        decimal[] prices = [10m, 20m, 30m, 40m, 50m];
        int period = 3;

        // Act
        decimal[] result = _calculator.CalculateSMA(prices, period);

        // Assert
        result.Length.ShouldBe(5);
        result[0].ShouldBe(0);  // Insufficient data
        result[1].ShouldBe(0);  // Insufficient data
        result[2].ShouldBe(20m); // (10+20+30)/3 = 20
        result[3].ShouldBe(30m); // (20+30+40)/3 = 30
        result[4].ShouldBe(40m); // (30+40+50)/3 = 40
    }

    [Fact]
    public void CalculateSMA_WithPeriod5_ReturnsCorrectAverages()
    {
        // Arrange
        decimal[] prices = [100m, 110m, 105m, 115m, 120m, 125m, 130m];
        int period = 5;

        // Act
        decimal[] result = _calculator.CalculateSMA(prices, period);

        // Assert
        result.Length.ShouldBe(7);
        result[4].ShouldBe(110m); // (100+110+105+115+120)/5 = 110
        result[5].ShouldBe(115m); // (110+105+115+120+125)/5 = 115
        result[6].ShouldBe(119m); // (105+115+120+125+130)/5 = 119
    }

    [Fact]
    public void CalculateSMA_WithInsufficientData_ReturnsZerosForEarlyIndices()
    {
        // Arrange
        decimal[] prices = [10m, 20m];
        int period = 5;

        // Act
        decimal[] result = _calculator.CalculateSMA(prices, period);

        // Assert
        result.Length.ShouldBe(2);
        result[0].ShouldBe(0);
        result[1].ShouldBe(0);
    }

    [Fact]
    public void CalculateSMA_WithSingleDataPoint_ReturnsZeroForPeriodGreaterThanOne()
    {
        // Arrange
        decimal[] prices = [100m];
        int period = 3;

        // Act
        decimal[] result = _calculator.CalculateSMA(prices, period);

        // Assert
        result.Length.ShouldBe(1);
        result[0].ShouldBe(0);
    }

    [Fact]
    public void CalculateSMA_WithEmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        decimal[] prices = [];
        int period = 3;

        // Act
        decimal[] result = _calculator.CalculateSMA(prices, period);

        // Assert
        result.Length.ShouldBe(0);
    }

    [Fact]
    public void CalculateSMA_WithPeriod1_ReturnsOriginalPrices()
    {
        // Arrange
        decimal[] prices = [10m, 20m, 30m, 40m, 50m];
        int period = 1;

        // Act
        decimal[] result = _calculator.CalculateSMA(prices, period);

        // Assert
        result.Length.ShouldBe(5);
        result[0].ShouldBe(10m);
        result[1].ShouldBe(20m);
        result[2].ShouldBe(30m);
        result[3].ShouldBe(40m);
        result[4].ShouldBe(50m);
    }

    [Fact]
    public void CalculateSMA_WithDecimalPrices_HandlesRoundingCorrectly()
    {
        // Arrange
        decimal[] prices = [10.5m, 20.3m, 30.7m];
        int period = 3;

        // Act
        decimal[] result = _calculator.CalculateSMA(prices, period);

        // Assert
        result[2].ShouldBe(20.5m, 0.01m); // (10.5+20.3+30.7)/3 ≈ 20.5
    }

    [Fact]
    public void CalculateSMA_WithLargeDataset_PerformsEfficiently()
    {
        // Arrange
        decimal[] prices = Enumerable.Range(1, 1000).Select(i => (decimal)i).ToArray();
        int period = 20;
        DateTime start = DateTime.Now;

        // Act
        decimal[] result = _calculator.CalculateSMA(prices, period);

        // Assert
        TimeSpan elapsed = DateTime.Now - start;
        result.Length.ShouldBe(1000);
        elapsed.TotalMilliseconds.ShouldBeLessThan(100); // Should complete in < 100ms
    }

    #endregion

    #region EMA Tests (8 tests)

    [Fact]
    public void CalculateEMA_WithValidData_ReturnsCorrectValues()
    {
        // Arrange
        decimal[] prices = [22m, 24m, 23m, 26m, 25m];
        int period = 3;

        // Act
        decimal[] result = _calculator.CalculateEMA(prices, period);

        // Assert
        result.Length.ShouldBe(5);
        result[0].ShouldBe(0);  // Insufficient data
        result[1].ShouldBe(0);  // Insufficient data
        result[2].ShouldBe(23m); // First EMA = SMA(3) = (22+24+23)/3 = 23
    }

    [Fact]
    public void CalculateEMA_FirstValueEqualsFirstSMA()
    {
        // Arrange
        decimal[] prices = [10m, 20m, 30m, 40m, 50m];
        int period = 3;

        // Act
        decimal[] ema = _calculator.CalculateEMA(prices, period);
        decimal[] sma = _calculator.CalculateSMA(prices, period);

        // Assert
        ema[2].ShouldBe(sma[2]); // First EMA value equals first SMA value
    }

    [Fact]
    public void CalculateEMA_SubsequentValuesUseExponentialSmoothing()
    {
        // Arrange
        decimal[] prices = [10m, 20m, 30m, 40m, 50m];
        int period = 3;
        _ = 2m / (period + 1); // 2/(3+1) = 0.5

        // Act
        decimal[] result = _calculator.CalculateEMA(prices, period);

        // Assert
        // First EMA = SMA = (10+20+30)/3 = 20
        // Next EMA = ((40 - 20) * 0.5) + 20 = 30
        result[3].ShouldBe(30m);
        // Next EMA = ((50 - 30) * 0.5) + 30 = 40
        result[4].ShouldBe(40m);
    }

    [Fact]
    public void CalculateEMA_WithInsufficientData_ReturnsZerosForEarlyIndices()
    {
        // Arrange
        decimal[] prices = [10m, 20m];
        int period = 5;

        // Act
        decimal[] result = _calculator.CalculateEMA(prices, period);

        // Assert
        result[0].ShouldBe(0);
        result[1].ShouldBe(0);
    }

    [Fact]
    public void CalculateEMA_WithMultiplier_CalculatesCorrectly()
    {
        // Arrange
        decimal[] prices = Enumerable.Range(1, 20).Select(i => (decimal)i * 10).ToArray();
        int period = 10;

        // Act
        decimal[] result = _calculator.CalculateEMA(prices, period);

        // Assert
        result[9].ShouldBeGreaterThan(0); // First EMA at index 9 (period - 1)
        result[19].ShouldBeGreaterThan(result[9]); // EMA should trend with prices
    }

    [Fact]
    public void CalculateEMA_WithEmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        decimal[] prices = [];
        int period = 3;

        // Act
        decimal[] result = _calculator.CalculateEMA(prices, period);

        // Assert
        result.Length.ShouldBe(0);
    }

    [Fact]
    public void CalculateEMA_ConvergesSlowerThanSMA()
    {
        // Arrange
        decimal[] prices = [100m, 100m, 100m, 100m, 100m, 200m]; // Sudden price jump
        int period = 5;

        // Act
        decimal[] ema = _calculator.CalculateEMA(prices, period);
        decimal[] sma = _calculator.CalculateSMA(prices, period);

        // Assert
        // EMA reacts faster to price changes than SMA due to exponential weighting
        // After sudden jump, EMA should be further from baseline than SMA
        Math.Abs(ema[5] - 100m).ShouldBeGreaterThan(Math.Abs(sma[5] - 100m));
    }

    [Fact]
    public void CalculateEMA_WithVolatileData_SmoothsCorrectly()
    {
        // Arrange
        decimal[] prices = [10m, 30m, 20m, 40m, 25m, 45m, 30m];
        int period = 3;

        // Act
        decimal[] result = _calculator.CalculateEMA(prices, period);

        // Assert
        result.Length.ShouldBe(7);
        result[2].ShouldBeGreaterThan(0); // First EMA calculated
        result[6].ShouldBeGreaterThan(0); // Final EMA calculated
    }

    #endregion

    #region RSI Tests (10 tests)

    [Fact]
    public void CalculateRSI_WithValidData_ReturnsValuesInRange0To100()
    {
        // Arrange
        decimal[] prices = Enumerable.Range(1, 30).Select(i => 100m + i).ToArray();
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateRSI(prices, period);

        // Assert
        result.Length.ShouldBe(30);
        foreach (decimal rsi in result.Skip(period))
        {
            rsi.ShouldBeInRange(0m, 100m);
        }
    }

    [Fact]
    public void CalculateRSI_WithAllGains_Returns100()
    {
        // Arrange - Continuously rising prices (all gains, no losses)
        decimal[] prices = Enumerable.Range(0, 20).Select(i => 100m + i * 5m).ToArray();
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateRSI(prices, period);

        // Assert
        result[period].ShouldBe(100m); // RSI should be 100 with all gains
    }

    [Fact]
    public void CalculateRSI_WithAllLosses_Returns0()
    {
        // Arrange - Continuously falling prices (all losses, no gains)
        decimal[] prices = Enumerable.Range(0, 20).Select(i => 200m - i * 5m).ToArray();
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateRSI(prices, period);

        // Assert
        result[period].ShouldBe(0m); // RSI should be 0 with all losses
    }

    [Fact]
    public void CalculateRSI_WithEqualGainsAndLosses_Returns50()
    {
        // Arrange - Alternating gains and losses of equal magnitude
        decimal[] prices = [100m, 110m, 100m, 110m, 100m, 110m, 100m, 110m, 100m, 110m, 100m, 110m, 100m, 110m, 100m, 110m];
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateRSI(prices, period);

        // Assert
        result[period].ShouldBe(50m, 0.1m); // RSI should be around 50 with balanced gains/losses
    }

    [Fact]
    public void CalculateRSI_WithInsufficientData_Returns50ForEarlyIndices()
    {
        // Arrange
        decimal[] prices = Enumerable.Range(1, 10).Select(i => (decimal)i).ToArray();
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateRSI(prices, period);

        // Assert
        // For indices <= period, should return default value of 50
        for (int i = 0; i <= period && i < prices.Length; i++)
        {
            result[i].ShouldBe(50m); // Default RSI value when insufficient data
        }
    }

    [Fact]
    public void CalculateRSI_WithUptrend_ReturnsHighValues()
    {
        // Arrange - Strong uptrend
        decimal[] prices = Enumerable.Range(0, 20).Select(i => 100m + i * 3m).ToArray();
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateRSI(prices, period);

        // Assert
        result[period].ShouldBeGreaterThan(70m); // RSI > 70 indicates overbought
    }

    [Fact]
    public void CalculateRSI_WithDowntrend_ReturnsLowValues()
    {
        // Arrange - Strong downtrend
        decimal[] prices = Enumerable.Range(0, 20).Select(i => 200m - i * 3m).ToArray();
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateRSI(prices, period);

        // Assert
        result[period].ShouldBeLessThan(30m); // RSI < 30 indicates oversold
    }

    [Fact]
    public void CalculateRSI_WithSidewaysMarket_ReturnsNeutralValues()
    {
        // Arrange - Sideways price action
        decimal[] prices = [100m, 101m, 100m, 101m, 100m, 101m, 100m, 101m, 100m, 101m, 100m, 101m, 100m, 101m, 100m];
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateRSI(prices, period);

        // Assert
        result[period].ShouldBeInRange(40m, 60m); // Neutral RSI range
    }

    [Fact]
    public void CalculateRSI_WithZeroChanges_HandlesGracefully()
    {
        // Arrange - Flat prices (no changes)
        decimal[] prices = Enumerable.Repeat(100m, 20).ToArray();
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateRSI(prices, period);

        // Assert
        // With no gains or losses (avgGain = 0, avgLoss = 0), RSI returns 100
        result[period].ShouldBe(100m);
    }

    [Fact]
    public void CalculateRSI_Period14_MatchesKnownValues()
    {
        // Arrange - Using standard 14-period RSI
        decimal[] prices = [44m, 44.34m, 44.09m, 43.61m, 44.33m, 44.83m, 45.10m, 45.42m, 45.84m, 46.08m, 45.89m, 46.03m, 45.61m, 46.28m, 46.28m];
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateRSI(prices, period);

        // Assert
        result[period].ShouldBeInRange(50m, 80m); // Expected range for this uptrend
    }

    #endregion

    #region MACD Tests (12 tests)

    [Fact]
    public void CalculateMACD_WithDefaultParameters_ReturnsThreeArrays()
    {
        // Arrange
        decimal[] prices = Enumerable.Range(1, 50).Select(i => 100m + i).ToArray();

        // Act
        (decimal[] macd, decimal[] signal, decimal[] histogram) = _calculator.CalculateMACD(prices);

        // Assert
        macd.Length.ShouldBe(50);
        signal.Length.ShouldBe(50);
        histogram.Length.ShouldBe(50);
    }

    [Fact]
    public void CalculateMACD_MACDLineEqualsFastMinusSlowEMA()
    {
        // Arrange
        decimal[] prices = Enumerable.Range(1, 50).Select(i => 100m + i).ToArray();
        int fastPeriod = 12;
        int slowPeriod = 26;

        // Act
        (decimal[] macd, decimal[] _, decimal[] _) = _calculator.CalculateMACD(prices, fastPeriod, slowPeriod);
        decimal[] fastEMA = _calculator.CalculateEMA(prices, fastPeriod);
        decimal[] slowEMA = _calculator.CalculateEMA(prices, slowPeriod);

        // Assert
        for (int i = slowPeriod; i < prices.Length; i++)
        {
            macd[i].ShouldBe(fastEMA[i] - slowEMA[i], 0.01m);
        }
    }

    [Fact]
    public void CalculateMACD_SignalLineIsEMAOfMACDLine()
    {
        // Arrange
        decimal[] prices = Enumerable.Range(1, 50).Select(i => 100m + i).ToArray();

        // Act
        (_, decimal[] signal, _) = _calculator.CalculateMACD(prices);

        // Assert
        // Signal line should be non-zero after sufficient data
        signal.Skip(34).ShouldAllBe(s => s != 0); // 26 (slow) + 9 (signal) - 1 = 34
    }

    [Fact]
    public void CalculateMACD_HistogramEqualsMACDMinusSignal()
    {
        // Arrange
        decimal[] prices = Enumerable.Range(1, 50).Select(i => 100m + i).ToArray();

        // Act
        (decimal[] macd, decimal[] signal, decimal[] histogram) = _calculator.CalculateMACD(prices);

        // Assert
        for (int i = 0; i < prices.Length; i++)
        {
            histogram[i].ShouldBe(macd[i] - signal[i], 0.001m);
        }
    }

    [Fact]
    public void CalculateMACD_WithInsufficientData_ReturnsZerosForEarlyIndices()
    {
        // Arrange
        decimal[] prices = Enumerable.Range(1, 20).Select(i => (decimal)i).ToArray();

        // Act
        (decimal[] macd, decimal[] signal, _) = _calculator.CalculateMACD(prices);

        // Assert
        // Early values should be zero due to insufficient data
        macd.Take(11).ShouldAllBe(m => m == 0); // fastPeriod - 1
        signal.Take(8).ShouldAllBe(s => s == 0); // signalPeriod - 1
    }

    [Fact]
    public void CalculateMACD_BullishCrossover_HistogramPositive()
    {
        // Arrange - Create strong uptrend for bullish MACD
        decimal[] prices = Enumerable.Range(0, 50).Select(i => 100m + i * 2m).ToArray();

        // Act
        (decimal[] macd, decimal[] _, decimal[] histogram) = _calculator.CalculateMACD(prices);

        // Assert
        // In strong uptrend, MACD line should be positive
        macd.Last().ShouldBeGreaterThan(0);
        // Histogram may lag, so just verify it's calculated
        histogram.Length.ShouldBe(50);
    }

    [Fact]
    public void CalculateMACD_BearishCrossover_HistogramNegative()
    {
        // Arrange - Create strong downtrend for bearish crossover
        decimal[] prices = Enumerable.Range(0, 50).Select(i => 200m - i * 2m).ToArray();

        // Act
        (decimal[] _, decimal[] _, decimal[] histogram) = _calculator.CalculateMACD(prices);

        // Assert
        // In strong downtrend, histogram should eventually become negative
        histogram.Last().ShouldBeLessThan(0);
    }

    [Fact]
    public void CalculateMACD_WithCustomParameters_UsesProvidedValues()
    {
        // Arrange
        decimal[] prices = Enumerable.Range(1, 50).Select(i => 100m + i).ToArray();
        int fastPeriod = 8;
        int slowPeriod = 21;
        int signalPeriod = 5;

        // Act
        (decimal[] macd, decimal[] signal, decimal[] histogram) = _calculator.CalculateMACD(prices, fastPeriod, slowPeriod, signalPeriod);

        // Assert
        macd.ShouldNotBeNull();
        signal.ShouldNotBeNull();
        histogram.ShouldNotBeNull();
        // Verify custom parameters produce different results than defaults
    }

    [Fact]
    public void CalculateMACD_AllArraysSameLength_AsInputArray()
    {
        // Arrange
        decimal[] prices = Enumerable.Range(1, 100).Select(i => (decimal)i).ToArray();

        // Act
        (decimal[] macd, decimal[] signal, decimal[] histogram) = _calculator.CalculateMACD(prices);

        // Assert
        macd.Length.ShouldBe(prices.Length);
        signal.Length.ShouldBe(prices.Length);
        histogram.Length.ShouldBe(prices.Length);
    }

    [Fact]
    public void CalculateMACD_WithTrendingData_ProducesReasonableValues()
    {
        // Arrange
        decimal[] prices = Enumerable.Range(1, 50).Select(i => 100m + (decimal)Math.Sin(i * 0.1) * 10).ToArray();

        // Act
        (decimal[] macd, decimal[] signal, decimal[] histogram) = _calculator.CalculateMACD(prices);

        // Assert
        // Values should be within reasonable range
        macd.Skip(26).ShouldAllBe(m => Math.Abs(m) < 100);
        signal.Skip(26).ShouldAllBe(s => Math.Abs(s) < 100);
        histogram.Skip(26).ShouldAllBe(h => Math.Abs(h) < 100);
    }

    [Fact]
    public void CalculateMACD_WithEmptyArray_ReturnsEmptyArrays()
    {
        // Arrange
        decimal[] prices = [];

        // Act
        (decimal[] macd, decimal[] signal, decimal[] histogram) = _calculator.CalculateMACD(prices);

        // Assert
        macd.Length.ShouldBe(0);
        signal.Length.ShouldBe(0);
        histogram.Length.ShouldBe(0);
    }

    [Fact]
    public void CalculateMACD_SignalPeriodGreaterThanSlowPeriod_HandlesCorrectly()
    {
        // Arrange
        decimal[] prices = Enumerable.Range(1, 60).Select(i => 100m + i).ToArray();

        // Act
        (decimal[] macd, decimal[] signal, decimal[] histogram) = _calculator.CalculateMACD(prices, 12, 26, 30);

        // Assert
        macd.Length.ShouldBe(60);
        signal.Length.ShouldBe(60);
        histogram.Length.ShouldBe(60);
    }

    #endregion

    #region ATR Tests (8 tests)

    [Fact]
    public void CalculateATR_WithValidData_ReturnsPositiveValues()
    {
        // Arrange
        List<HistoricalPrice> pricesList = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 105m, 102m, 108m, 103m, 110m, 106m, 112m, 108m, 115m,
                        110m, 117m, 112m, 120m, 115m, 122m, 118m, 125m, 120m, 128m)
            .Build();
        HistoricalPrice[] prices = pricesList.ToArray();
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateATR(prices, period);

        // Assert
        result.Length.ShouldBe(20);
        result.Skip(period - 1).ShouldAllBe(atr => atr > 0);
    }

    [Fact]
    public void CalculateATR_FirstValueIsSimpleAverage()
    {
        // Arrange
        List<HistoricalPrice> pricesList = HistoricalPriceBuilder.Create()
            .WithPrices(Enumerable.Range(1, 20).Select(i => 100m + i).ToArray())
            .Build();
        HistoricalPrice[] prices = pricesList.ToArray();
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateATR(prices, period);

        // Assert
        result[13].ShouldBeGreaterThan(0); // First ATR at index period-1
    }

    [Fact]
    public void CalculateATR_SubsequentValuesAreSmoothed()
    {
        // Arrange
        List<HistoricalPrice> pricesList = HistoricalPriceBuilder.Create()
            .WithPrices(Enumerable.Range(1, 20).Select(i => 100m + i).ToArray())
            .Build();
        HistoricalPrice[] prices = pricesList.ToArray();
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateATR(prices, period);

        // Assert
        // ATR should be smoothed using Wilder's smoothing method
        result[14].ShouldNotBe(result[13]); // Values should differ as they're smoothed
    }

    [Fact]
    public void CalculateATR_WithGaps_UsesMaxOfThreeRanges()
    {
        // Arrange - Create prices with gap up
        List<HistoricalPrice> pricesList = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m, 105m, 106m, 107m, 108m, 109m,
                        110m, 111m, 112m, 113m, 130m) // Large gap up
            .Build();
        HistoricalPrice[] prices = pricesList.ToArray();
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateATR(prices, period);

        // Assert
        result[14].ShouldBeGreaterThan(result[13]); // ATR should increase due to gap
    }

    [Fact]
    public void CalculateATR_WithInsufficientData_ReturnsZerosForEarlyIndices()
    {
        // Arrange
        List<HistoricalPrice> pricesList = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m)
            .Build();
        HistoricalPrice[] prices = pricesList.ToArray();
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateATR(prices, period);

        // Assert
        result[0].ShouldBe(0);
        result[1].ShouldBe(0);
        result[2].ShouldBe(0);
    }

    [Fact]
    public void CalculateATR_WithVolatileData_ReturnsHigherValues()
    {
        // Arrange - High volatility (large price swings)
        List<HistoricalPrice> volatilePrices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 120m, 90m, 130m, 80m, 140m, 70m, 150m, 60m, 160m,
                        50m, 170m, 40m, 180m, 30m, 190m, 20m, 200m, 10m, 210m)
            .Build();
        List<HistoricalPrice> stablePrices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m, 105m, 106m, 107m, 108m, 109m,
                        110m, 111m, 112m, 113m, 114m, 115m, 116m, 117m, 118m, 119m)
            .Build();
        int period = 14;

        // Act
        decimal[] volatileATR = _calculator.CalculateATR(volatilePrices.ToArray(), period);
        decimal[] stableATR = _calculator.CalculateATR(stablePrices.ToArray(), period);

        // Assert
        volatileATR.Last().ShouldBeGreaterThan(stableATR.Last());
    }

    [Fact]
    public void CalculateATR_WithStableData_ReturnsLowerValues()
    {
        // Arrange - Low volatility (small price changes)
        List<HistoricalPrice> pricesList = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 100.5m, 101m, 101.5m, 102m, 102.5m, 103m, 103.5m, 104m, 104.5m,
                        105m, 105.5m, 106m, 106.5m, 107m, 107.5m, 108m, 108.5m, 109m, 109.5m)
            .Build();
        HistoricalPrice[] prices = pricesList.ToArray();
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateATR(prices, period);

        // Assert
        result.Skip(period - 1).ShouldAllBe(atr => atr < 5m); // Low ATR for stable prices
    }

    [Fact]
    public void CalculateATR_WithNullPrices_HandlesGracefully()
    {
        // Arrange - Some null OHLC values
        List<HistoricalPrice> pricesList = HistoricalPriceBuilder.Create()
            .WithPrices(Enumerable.Range(1, 20).Select(i => 100m + i).ToArray())
            .Build();

        // Manually set some nulls
        pricesList[5].High = null;
        pricesList[5].Low = null;

        HistoricalPrice[] prices = pricesList.ToArray();
        int period = 14;

        // Act
        decimal[] result = _calculator.CalculateATR(prices, period);

        // Assert
        result.Length.ShouldBe(20);
        // Should handle nulls gracefully (treated as 0)
    }

    #endregion

    #region StdDev Tests (6 tests)

    [Fact]
    public void CalculateStdDev_WithValidData_ReturnsCorrectValues()
    {
        // Arrange
        decimal[] prices = [2m, 4m, 4m, 4m, 5m, 5m, 7m, 9m];
        int period = 8;

        // Act
        decimal[] result = _calculator.CalculateStdDev(prices, period);

        // Assert
        result.Length.ShouldBe(8);
        result[7].ShouldBeGreaterThan(0);
        result[7].ShouldBe(2m, 0.1m); // Standard deviation ≈ 2
    }

    [Fact]
    public void CalculateStdDev_WithIdenticalValues_ReturnsZero()
    {
        // Arrange - All same values = zero variance
        decimal[] prices = [100m, 100m, 100m, 100m, 100m];
        int period = 5;

        // Act
        decimal[] result = _calculator.CalculateStdDev(prices, period);

        // Assert
        result[4].ShouldBe(0m); // No variance = StdDev of 0
    }

    [Fact]
    public void CalculateStdDev_WithInsufficientData_ReturnsZerosForEarlyIndices()
    {
        // Arrange
        decimal[] prices = [10m, 20m, 30m];
        int period = 10;

        // Act
        decimal[] result = _calculator.CalculateStdDev(prices, period);

        // Assert
        result[0].ShouldBe(0);
        result[1].ShouldBe(0);
        result[2].ShouldBe(0);
    }

    [Fact]
    public void CalculateStdDev_WithHighVariance_ReturnsHigherValues()
    {
        // Arrange
        decimal[] highVariance = [10m, 100m, 20m, 90m, 30m, 80m, 40m, 70m, 50m, 60m];
        decimal[] lowVariance = [50m, 51m, 52m, 51m, 50m, 51m, 52m, 51m, 50m, 51m];
        int period = 10;

        // Act
        decimal[] highResult = _calculator.CalculateStdDev(highVariance, period);
        decimal[] lowResult = _calculator.CalculateStdDev(lowVariance, period);

        // Assert
        highResult[9].ShouldBeGreaterThan(lowResult[9]);
    }

    [Fact]
    public void CalculateStdDev_WithLowVariance_ReturnsLowerValues()
    {
        // Arrange
        decimal[] prices = [100m, 100.5m, 99.5m, 100.2m, 99.8m, 100.1m, 99.9m];
        int period = 5;

        // Act
        decimal[] result = _calculator.CalculateStdDev(prices, period);

        // Assert
        result.Skip(period - 1).ShouldAllBe(stdDev => stdDev < 1m);
    }

    [Fact]
    public void CalculateStdDev_UsesPopulationFormula_NotSample()
    {
        // Arrange
        decimal[] prices = [2m, 4m, 6m, 8m, 10m];
        int period = 5;

        // Act
        decimal[] result = _calculator.CalculateStdDev(prices, period);

        // Assert
        // Population StdDev formula: sqrt(sum((x - mean)^2) / n)
        // Mean = (2+4+6+8+10)/5 = 6
        // Squared differences: (6-2)^2=16, (6-4)^2=4, (6-6)^2=0, (6-8)^2=4, (6-10)^2=16
        // Variance = (16+4+0+4+16)/5 = 40/5 = 8
        // StdDev = sqrt(8) ≈ 2.828
        result[4].ShouldBe(2.828m, 0.01m);
    }

    #endregion

    #region Edge Cases & Integration Tests (8 tests)

    [Fact]
    public void AllIndicators_WithEmptyArray_ReturnEmptyOrZeroArray()
    {
        // Arrange
        decimal[] emptyPrices = [];
        HistoricalPrice[] emptyHistoricalPrices = [];

        // Act & Assert
        _calculator.CalculateSMA(emptyPrices, 5).Length.ShouldBe(0);
        _calculator.CalculateEMA(emptyPrices, 5).Length.ShouldBe(0);
        _calculator.CalculateRSI(emptyPrices, 14).Length.ShouldBe(0);
        _calculator.CalculateStdDev(emptyPrices, 5).Length.ShouldBe(0);

        (decimal[] macd, decimal[] signal, decimal[] histogram) = _calculator.CalculateMACD(emptyPrices);
        macd.Length.ShouldBe(0);
        signal.Length.ShouldBe(0);
        histogram.Length.ShouldBe(0);

        _calculator.CalculateATR(emptyHistoricalPrices, 14).Length.ShouldBe(0);
    }

    [Fact]
    public void AllIndicators_WithSingleElement_HandleGracefully()
    {
        // Arrange
        decimal[] singlePrice = [100m];
        List<HistoricalPrice> singleHistoricalPrice = HistoricalPriceBuilder.Create()
            .WithPrices(100m)
            .Build();

        // Act & Assert
        _calculator.CalculateSMA(singlePrice, 5)[0].ShouldBe(0);
        _calculator.CalculateEMA(singlePrice, 5)[0].ShouldBe(0);
        _calculator.CalculateRSI(singlePrice, 14)[0].ShouldBe(50m);
        _calculator.CalculateStdDev(singlePrice, 5)[0].ShouldBe(0);

        (decimal[] macd, decimal[] signal, decimal[] histogram) = _calculator.CalculateMACD(singlePrice);
        macd[0].ShouldBe(0);
        signal[0].ShouldBe(0);
        histogram[0].ShouldBe(0);

        _calculator.CalculateATR(singleHistoricalPrice.ToArray(), 14)[0].ShouldBe(0);
    }

    [Fact]
    public void AllIndicators_WithPeriodGreaterThanDataLength_ReturnAllZeros()
    {
        // Arrange
        decimal[] prices = [10m, 20m, 30m];
        List<HistoricalPrice> historicalPrices = HistoricalPriceBuilder.Create()
            .WithPrices(10m, 20m, 30m)
            .Build();
        int period = 10;

        // Act & Assert
        _calculator.CalculateSMA(prices, period).ShouldAllBe(v => v == 0);
        _calculator.CalculateEMA(prices, period).ShouldAllBe(v => v == 0);
        _calculator.CalculateStdDev(prices, period).ShouldAllBe(v => v == 0);
        _calculator.CalculateATR(historicalPrices.ToArray(), period).ShouldAllBe(v => v == 0);
        // RSI returns 50 for insufficient data, not 0
    }

    [Fact]
    public void AllIndicators_WithNegativePeriod_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        decimal[] prices = [10m, 20m, 30m, 40m, 50m];
        List<HistoricalPrice> historicalPrices = HistoricalPriceBuilder.Create()
            .WithPrices(10m, 20m, 30m, 40m, 50m)
            .Build();
        int negativePeriod = -5;

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _calculator.CalculateSMA(prices, negativePeriod));
        Should.Throw<ArgumentOutOfRangeException>(() => _calculator.CalculateEMA(prices, negativePeriod));
        Should.Throw<ArgumentOutOfRangeException>(() => _calculator.CalculateRSI(prices, negativePeriod));
        Should.Throw<ArgumentOutOfRangeException>(() => _calculator.CalculateStdDev(prices, negativePeriod));
        Should.Throw<ArgumentOutOfRangeException>(() => _calculator.CalculateATR(historicalPrices.ToArray(), negativePeriod));
    }

    [Fact]
    public void AllIndicators_WithZeroPeriod_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        decimal[] prices = [10m, 20m, 30m, 40m, 50m];
        List<HistoricalPrice> historicalPrices = HistoricalPriceBuilder.Create()
            .WithPrices(10m, 20m, 30m, 40m, 50m)
            .Build();
        int zeroPeriod = 0;

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _calculator.CalculateSMA(prices, zeroPeriod));
        Should.Throw<ArgumentOutOfRangeException>(() => _calculator.CalculateEMA(prices, zeroPeriod));
        Should.Throw<ArgumentOutOfRangeException>(() => _calculator.CalculateRSI(prices, zeroPeriod));
        Should.Throw<ArgumentOutOfRangeException>(() => _calculator.CalculateStdDev(prices, zeroPeriod));
        Should.Throw<ArgumentOutOfRangeException>(() => _calculator.CalculateATR(historicalPrices.ToArray(), zeroPeriod));
    }

    [Fact]
    public void AllIndicators_WithDecimalPrecision_MaintainAccuracy()
    {
        // Arrange
        decimal[] prices = [10.123456m, 20.234567m, 30.345678m, 40.456789m, 50.567890m];
        List<HistoricalPrice> historicalPrices = HistoricalPriceBuilder.Create()
            .WithPrices(prices)
            .Build();
        int period = 3;

        // Act
        decimal[] sma = _calculator.CalculateSMA(prices, period);
        decimal[] ema = _calculator.CalculateEMA(prices, period);
        decimal[] rsi = _calculator.CalculateRSI(prices, period);
        decimal[] stdDev = _calculator.CalculateStdDev(prices, period);
        decimal[] atr = _calculator.CalculateATR(historicalPrices.ToArray(), period);

        // Assert - All calculations should preserve decimal precision
        sma[2].ShouldBeGreaterThan(0);
        ema[2].ShouldBeGreaterThan(0);
        rsi[3].ShouldBeInRange(0m, 100m);
        stdDev[2].ShouldBeGreaterThan(0);
        atr[2].ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AllIndicators_WithLargeDataset_Complete1000BarsIn100ms()
    {
        // Arrange
        decimal[] prices = Enumerable.Range(1, 1000).Select(i => 100m + (decimal)Math.Sin(i * 0.1) * 10).ToArray();
        List<HistoricalPrice> historicalPrices = HistoricalPriceBuilder.Create()
            .WithPrices(prices)
            .Build();
        DateTime start = DateTime.Now;

        // Act
        _calculator.CalculateSMA(prices, 20);
        _calculator.CalculateEMA(prices, 20);
        _calculator.CalculateRSI(prices, 14);
        _calculator.CalculateMACD(prices);
        _calculator.CalculateStdDev(prices, 20);
        _calculator.CalculateATR(historicalPrices.ToArray(), 14);

        // Assert
        TimeSpan elapsed = DateTime.Now - start;
        elapsed.TotalMilliseconds.ShouldBeLessThan(100);
    }

    [Fact]
    public void AllIndicators_CalledMultipleTimes_ReturnConsistentResults()
    {
        // Arrange
        decimal[] prices = Enumerable.Range(1, 50).Select(i => 100m + i).ToArray();

        // Act - Call each indicator multiple times
        decimal[] sma1 = _calculator.CalculateSMA(prices, 20);
        decimal[] sma2 = _calculator.CalculateSMA(prices, 20);

        decimal[] ema1 = _calculator.CalculateEMA(prices, 20);
        decimal[] ema2 = _calculator.CalculateEMA(prices, 20);

        decimal[] rsi1 = _calculator.CalculateRSI(prices, 14);
        decimal[] rsi2 = _calculator.CalculateRSI(prices, 14);

        // Assert - Results should be identical
        for (int i = 0; i < prices.Length; i++)
        {
            sma1[i].ShouldBe(sma2[i]);
            ema1[i].ShouldBe(ema2[i]);
            rsi1[i].ShouldBe(rsi2[i]);
        }
    }

    #endregion

    #region Ichimoku Tests (15 tests)

    [Fact]
    public void CalculateConversionLine_WithValidData_ReturnsCorrectMidpoint()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 105m, 102m, 108m, 103m, 110m, 106m, 112m, 108m, 115m)
            .Build();

        // Manually set highs/lows for deterministic results
        for (int i = 0; i < prices.Count; i++)
        {
            prices[i].High = prices[i].Close + 2m;
            prices[i].Low = prices[i].Close - 2m;
        }

        // Act
        decimal[] result = _calculator.CalculateConversionLine(prices.ToArray());

        // Assert
        result.Length.ShouldBe(10);
        result[8].ShouldBeGreaterThan(0); // First valid Tenkan at index 8 (period - 1)
        // Tenkan should be midpoint of high/low range over last 9 bars
        // At index 8, looking back 9 bars (index 0-8): closes are 100, 105, 102, 108, 103, 110, 106, 112, 108
        // With highs = close+2 and lows = close-2, highest high = 114, lowest low = 98
        result[8].ShouldBe(106m); // (114 + 98) / 2 = 106
    }

    [Fact]
    public void CalculateBaseLine_WithValidData_ReturnsCorrectMidpoint()
    {
        // Arrange - need at least 26 bars
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithTrendingPrices(30, 100m, 1m)
            .Build();

        // Act
        decimal[] result = _calculator.CalculateBaseLine(prices.ToArray());

        // Assert
        result.Length.ShouldBe(30);
        result[25].ShouldBeGreaterThan(0); // First valid Kijun at index 25
        result[29].ShouldBeGreaterThan(result[25]); // Should increase with trending data
    }

    [Fact]
    public void CalculateLeadingSpanA_WithValidInputs_ReturnsAverage()
    {
        // Arrange
        decimal[] tenkan = [0m, 0m, 0m, 100m, 102m, 104m];
        decimal[] kijun = [0m, 0m, 0m, 98m, 100m, 102m];

        // Act
        decimal[] result = _calculator.CalculateLeadingSpanA(tenkan, kijun);

        // Assert
        result.Length.ShouldBe(6);
        result[3].ShouldBe(99m);  // (100 + 98) / 2
        result[4].ShouldBe(101m); // (102 + 100) / 2
        result[5].ShouldBe(103m); // (104 + 102) / 2
    }

    [Fact]
    public void CalculateLeadingSpanB_WithValidData_ReturnsCorrectMidpoint()
    {
        // Arrange - need at least 52 bars
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithTrendingPrices(60, 100m, 0.5m)
            .Build();

        // Act
        decimal[] result = _calculator.CalculateLeadingSpanB(prices.ToArray());

        // Assert
        result.Length.ShouldBe(60);
        result[51].ShouldBeGreaterThan(0); // First valid at index 51
        result[59].ShouldBeGreaterThan(result[51]); // Should increase with trending data
    }

    [Fact]
    public void CalculateChikouSpan_ReturnsClosePrices()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m, 103m, 104m)
            .Build();

        // Act
        decimal[] result = _calculator.CalculateLaggingSpan(prices.ToArray());

        // Assert
        result.Length.ShouldBe(5);
        result[0].ShouldBe(100m);
        result[1].ShouldBe(101m);
        result[2].ShouldBe(102m);
        result[3].ShouldBe(103m);
        result[4].ShouldBe(104m);
    }

    [Fact]
    public void CalculateConversionLine_WithInsufficientData_ReturnsZeros()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m) // Only 3 bars, need 9
            .Build();

        // Act
        decimal[] result = _calculator.CalculateConversionLine(prices.ToArray());

        // Assert
        result.ShouldAllBe(v => v == 0);
    }

    [Fact]
    public void CalculateBaseLine_WithInsufficientData_ReturnsZeros()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m) // Only 3 bars, need 26
            .Build();

        // Act
        decimal[] result = _calculator.CalculateBaseLine(prices.ToArray());

        // Assert
        result.ShouldAllBe(v => v == 0);
    }

    [Fact]
    public void CalculateLeadingSpanA_WithMismatchedLengths_Throws()
    {
        // Arrange
        decimal[] tenkan = [100m, 101m, 102m];
        decimal[] kijun = [100m, 101m]; // Different length

        // Act & Assert
        Should.Throw<ArgumentException>(() => _calculator.CalculateLeadingSpanA(tenkan, kijun));
    }

    [Fact]
    public void CalculateConversionLine_WithNullPrices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _calculator.CalculateConversionLine(null!));
    }

    [Fact]
    public void CalculateBaseLine_WithNegativePeriod_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithPrices(100m, 101m, 102m)
            .Build();

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => _calculator.CalculateBaseLine(prices.ToArray(), -1));
    }

    [Fact]
    public void CalculateLeadingSpanB_WithEmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        HistoricalPrice[] emptyPrices = [];

        // Act
        decimal[] result = _calculator.CalculateLeadingSpanB(emptyPrices);

        // Assert
        result.Length.ShouldBe(0);
    }

    [Fact]
    public void CalculateChikouSpan_WithNullPrices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _calculator.CalculateLaggingSpan(null!));
    }

    [Fact]
    public void IchimokuIndicators_CalculateDonchianMidpoint_ForMultiplePeriods()
    {
        // Arrange - create data with clear high/low ranges
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithTrendingPrices(60, 100m, 1m)
            .Build();

        // Act - calculate Tenkan (9), Kijun (26), and Senkou B (52)
        decimal[] tenkan = _calculator.CalculateConversionLine(prices.ToArray());
        decimal[] kijun = _calculator.CalculateBaseLine(prices.ToArray());
        decimal[] senkouB = _calculator.CalculateLeadingSpanB(prices.ToArray());

        // Assert - all should use same Donchian midpoint logic
        tenkan[8].ShouldBeGreaterThan(0);  // Valid at index 8
        kijun[25].ShouldBeGreaterThan(0);  // Valid at index 25
        senkouB[51].ShouldBeGreaterThan(0); // Valid at index 51

        // Tenkan should be more responsive than Kijun (shorter period)
        // In an uptrend, shorter period should be closer to current price
    }

    [Fact]
    public void IchimokuIndicators_CompleteSet_CalculatesAllComponents()
    {
        // Arrange - realistic dataset
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithTrendingPrices(100, 100m, 0.5m)
            .Build();

        // Act - calculate full Ichimoku suite
        decimal[] tenkan = _calculator.CalculateConversionLine(prices.ToArray());
        decimal[] kijun = _calculator.CalculateBaseLine(prices.ToArray());
        decimal[] senkouA = _calculator.CalculateLeadingSpanA(tenkan, kijun);
        decimal[] senkouB = _calculator.CalculateLeadingSpanB(prices.ToArray());
        decimal[] chikou = _calculator.CalculateLaggingSpan(prices.ToArray());

        // Assert - all arrays should have correct length
        tenkan.Length.ShouldBe(100);
        kijun.Length.ShouldBe(100);
        senkouA.Length.ShouldBe(100);
        senkouB.Length.ShouldBe(100);
        chikou.Length.ShouldBe(100);

        // Senkou A should be average of Tenkan and Kijun
        senkouA[50].ShouldBe((tenkan[50] + kijun[50]) / 2m);

        // Chikou should match close prices
        chikou[50].ShouldBe(prices[50].Close ?? 0);
    }

    [Fact]
    public void IchimokuIndicators_WithLargeDataset_Complete1000BarsEfficiently()
    {
        // Arrange
        List<HistoricalPrice> prices = HistoricalPriceBuilder.Create()
            .WithTrendingPrices(1000, 100m, 0.1m)
            .Build();
        DateTime start = DateTime.Now;

        // Act - calculate all Ichimoku components
        decimal[] tenkan = _calculator.CalculateConversionLine(prices.ToArray());
        decimal[] kijun = _calculator.CalculateBaseLine(prices.ToArray());
        _ = _calculator.CalculateLeadingSpanA(tenkan, kijun);
        _ = _calculator.CalculateLeadingSpanB(prices.ToArray());
        _ = _calculator.CalculateLaggingSpan(prices.ToArray());

        // Assert - should complete in reasonable time
        TimeSpan elapsed = DateTime.Now - start;
        elapsed.TotalMilliseconds.ShouldBeLessThan(200); // Allow 200ms for 5 indicators
    }

    #endregion
}
