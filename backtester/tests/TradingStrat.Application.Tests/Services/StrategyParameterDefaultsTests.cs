using Shouldly;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Strategies;

namespace TradingStrat.Application.Tests.Services;

public class StrategyParameterDefaultsTests
{
    private readonly StrategyParameterDefaults _service;

    public StrategyParameterDefaultsTests()
    {
        _service = new StrategyParameterDefaults();
    }

    [Fact]
    public void GetDefault_ForRSIPeriod_Returns14()
    {
        // Act
        int period = _service.GetDefault<int>(StrategyType.RSI, "Period");

        // Assert
        period.ShouldBe(14);
    }

    [Fact]
    public void GetDefault_ForRSIOversoldThreshold_Returns30()
    {
        // Act
        decimal oversold = _service.GetDefault<decimal>(StrategyType.RSI, "OversoldThreshold");

        // Assert
        oversold.ShouldBe(30m);
    }

    [Fact]
    public void GetDefault_ForRSIOverboughtThreshold_Returns70()
    {
        // Act
        decimal overbought = _service.GetDefault<decimal>(StrategyType.RSI, "OverboughtThreshold");

        // Assert
        overbought.ShouldBe(70m);
    }

    [Fact]
    public void GetAllDefaults_ForRSI_ReturnsAllThreeParameters()
    {
        // Act
        Dictionary<string, object> defaults = _service.GetAllDefaults(StrategyType.RSI);

        // Assert
        defaults.ShouldNotBeNull();
        defaults.Count.ShouldBe(3);
        defaults["Period"].ShouldBe(14);
        defaults["OversoldThreshold"].ShouldBe(30m);
        defaults["OverboughtThreshold"].ShouldBe(70m);
    }

    [Fact]
    public void GetDefault_ForMACrossFastPeriod_Returns20()
    {
        // Act
        int fastPeriod = _service.GetDefault<int>(StrategyType.MovingAverageCrossover, "FastPeriod");

        // Assert
        fastPeriod.ShouldBe(20);
    }

    [Fact]
    public void GetDefault_ForMACrossSlowPeriod_Returns50()
    {
        // Act
        int slowPeriod = _service.GetDefault<int>(StrategyType.MovingAverageCrossover, "SlowPeriod");

        // Assert
        slowPeriod.ShouldBe(50);
    }

    [Fact]
    public void GetDefault_ForMACDFastPeriod_Returns12()
    {
        // Act
        int fastPeriod = _service.GetDefault<int>(StrategyType.MACD, "FastPeriod");

        // Assert
        fastPeriod.ShouldBe(12);
    }

    [Fact]
    public void GetDefault_ForMACDSlowPeriod_Returns26()
    {
        // Act
        int slowPeriod = _service.GetDefault<int>(StrategyType.MACD, "SlowPeriod");

        // Assert
        slowPeriod.ShouldBe(26);
    }

    [Fact]
    public void GetDefault_ForMACDSignalPeriod_Returns9()
    {
        // Act
        int signalPeriod = _service.GetDefault<int>(StrategyType.MACD, "SignalPeriod");

        // Assert
        signalPeriod.ShouldBe(9);
    }

    [Fact]
    public void GetRange_ForRSIPeriod_ReturnsCorrectRange()
    {
        // Act
        (int min, int max) = _service.GetRange<int>(StrategyType.RSI, "Period");

        // Assert
        min.ShouldBe(2);
        max.ShouldBe(100);
    }

    [Fact]
    public void GetRange_ForRSIOversold_ReturnsCorrectRange()
    {
        // Act
        (decimal min, decimal max) = _service.GetRange<decimal>(StrategyType.RSI, "OversoldThreshold");

        // Assert
        min.ShouldBe(0m);
        max.ShouldBe(50m);
    }

    [Fact]
    public void GetRange_ForRSIOverbought_ReturnsCorrectRange()
    {
        // Act
        (decimal min, decimal max) = _service.GetRange<decimal>(StrategyType.RSI, "OverboughtThreshold");

        // Assert
        min.ShouldBe(50m);
        max.ShouldBe(100m);
    }

    [Fact]
    public void GetParameters_ForUnknownStrategy_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _service.GetParameters((StrategyType)999));
    }

    [Fact]
    public void GetDefault_ForNonExistentParameter_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        Should.Throw<KeyNotFoundException>(() =>
            _service.GetDefault<int>(StrategyType.RSI, "NonExistentParameter"));
    }

    [Fact]
    public void TryGetParameter_WithValidParameter_ReturnsTrue()
    {
        // Act
        bool result = _service.TryGetParameter(
            StrategyType.RSI,
            "Period",
            out ParameterMetadata? parameter);

        // Assert
        result.ShouldBeTrue();
        parameter.ShouldNotBeNull();
        parameter.Name.ShouldBe("Period");
        parameter.DefaultValue.ShouldBe(14);
    }

    [Fact]
    public void TryGetParameter_WithInvalidParameter_ReturnsFalse()
    {
        // Act
        bool result = _service.TryGetParameter(
            StrategyType.RSI,
            "NonExistent",
            out ParameterMetadata? parameter);

        // Assert
        result.ShouldBeFalse();
        parameter.ShouldBeNull();
    }

    [Fact]
    public void GetParameters_ForRSI_ReturnsStrategyParameters()
    {
        // Act
        StrategyParameters parameters = _service.GetParameters(StrategyType.RSI);

        // Assert
        parameters.ShouldNotBeNull();
        parameters.Get("Period").ShouldNotBeNull();
        parameters.Get("OversoldThreshold").ShouldNotBeNull();
        parameters.Get("OverboughtThreshold").ShouldNotBeNull();
    }

    [Fact]
    public void ParameterMetadata_HasCorrectMetadata()
    {
        // Act
        StrategyParameters parameters = _service.GetParameters(StrategyType.RSI);
        ParameterMetadata period = parameters.Get("Period");

        // Assert
        period.Name.ShouldBe("Period");
        period.DisplayName.ShouldBe("RSI Period");
        period.Description.ShouldContain("periods");
        period.Type.ShouldBe("int");
        period.DefaultValue.ShouldBe(14);
        period.MinValue.ShouldBe(2);
        period.MaxValue.ShouldBe(100);
    }

    [Fact]
    public void GetDefault_ForMLBuyThreshold_ReturnsCorrectValue()
    {
        // Act
        decimal buyThreshold = _service.GetDefault<decimal>(StrategyType.MachineLearning, "BuyThreshold");

        // Assert
        buyThreshold.ShouldBe(0.01m);
    }

    [Fact]
    public void GetDefault_ForMLSellThreshold_ReturnsCorrectValue()
    {
        // Act
        decimal sellThreshold = _service.GetDefault<decimal>(StrategyType.MachineLearning, "SellThreshold");

        // Assert
        sellThreshold.ShouldBe(-0.01m);
    }

    [Fact]
    public void GetDefault_ForIchimokuConversionLinePeriod_Returns9()
    {
        // Act
        int period = _service.GetDefault<int>(StrategyType.Ichimoku, "ConversionLinePeriod");

        // Assert
        period.ShouldBe(9);
    }

    [Fact]
    public void GetAllDefaults_ForMovingAverageCrossover_ReturnsCorrectDefaults()
    {
        // Act
        Dictionary<string, object> defaults = _service.GetAllDefaults(StrategyType.MovingAverageCrossover);

        // Assert
        defaults.ShouldNotBeNull();
        defaults.Count.ShouldBe(2);
        defaults["FastPeriod"].ShouldBe(20);
        defaults["SlowPeriod"].ShouldBe(50);
    }

    [Fact]
    public void GetAllDefaults_ForMACD_ReturnsCorrectDefaults()
    {
        // Act
        Dictionary<string, object> defaults = _service.GetAllDefaults(StrategyType.MACD);

        // Assert
        defaults.ShouldNotBeNull();
        defaults.Count.ShouldBe(3);
        defaults["FastPeriod"].ShouldBe(12);
        defaults["SlowPeriod"].ShouldBe(26);
        defaults["SignalPeriod"].ShouldBe(9);
    }

    [Fact]
    public void GetAllDefaults_ForIchimoku_ReturnsAllSixParameters()
    {
        // Act
        Dictionary<string, object> defaults = _service.GetAllDefaults(StrategyType.Ichimoku);

        // Assert
        defaults.ShouldNotBeNull();
        defaults.Count.ShouldBe(6);
        defaults["ConversionLinePeriod"].ShouldBe(9);
        defaults["BaseLinePeriod"].ShouldBe(26);
        defaults["LeadingSpanBPeriod"].ShouldBe(52);
        defaults["Displacement"].ShouldBe(26);
        defaults["CrossLookbackDays"].ShouldBe(5);
        defaults["RiskPercentage"].ShouldBe(0.02m);
    }
}
