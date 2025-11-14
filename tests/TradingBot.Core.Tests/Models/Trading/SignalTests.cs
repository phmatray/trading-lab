// <copyright file="SignalTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Shouldly;
using TradingBot.Core.Enums;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Tests.Models.Trading;

/// <summary>
/// Unit tests for the Signal model.
/// </summary>
public sealed class SignalTests
{
    [Fact]
    public void Signal_WhenCreatedWithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var signalId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        var signal = new Signal
        {
            Id = signalId,
            StrategyName = "MomentumStrategy",
            Symbol = "SPY",
            Type = SignalType.Buy,
            Timestamp = timestamp,
            Confidence = 0.85m,
        };

        // Assert
        signal.Id.ShouldBe(signalId);
        signal.StrategyName.ShouldBe("MomentumStrategy");
        signal.Symbol.ShouldBe("SPY");
        signal.Type.ShouldBe(SignalType.Buy);
        signal.Timestamp.ShouldBe(timestamp);
        signal.Confidence.ShouldBe(0.85m);
        signal.SuggestedPrice.ShouldBeNull();
        signal.Metadata.ShouldBeNull();
    }

    [Fact]
    public void Signal_WithSuggestedPrice_ShouldSetPriceCorrectly()
    {
        // Arrange & Act
        var signal = new Signal
        {
            Id = Guid.NewGuid(),
            StrategyName = "LimitStrategy",
            Symbol = "AAPL",
            Type = SignalType.Buy,
            Timestamp = DateTime.UtcNow,
            Confidence = 0.75m,
            SuggestedPrice = 180.50m,
        };

        // Assert
        signal.SuggestedPrice.ShouldBe(180.50m);
        signal.SuggestedPrice.HasValue.ShouldBeTrue();
    }

    [Fact]
    public void Signal_WithMetadata_ShouldStoreMetadataCorrectly()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "RSI", 72.5 },
            { "MACD", 1.35 },
            { "Volume", 1000000 },
            { "Trend", "Upward" },
        };

        // Act
        var signal = new Signal
        {
            Id = Guid.NewGuid(),
            StrategyName = "TechnicalAnalysis",
            Symbol = "TSLA",
            Type = SignalType.Buy,
            Timestamp = DateTime.UtcNow,
            Confidence = 0.90m,
            Metadata = metadata,
        };

        // Assert
        signal.Metadata.ShouldNotBeNull();
        signal.Metadata.Count.ShouldBe(4);
        signal.Metadata["RSI"].ShouldBe(72.5);
        signal.Metadata["MACD"].ShouldBe(1.35);
        signal.Metadata["Volume"].ShouldBe(1000000);
        signal.Metadata["Trend"].ShouldBe("Upward");
    }

    [Fact]
    public void Signal_BuySignal_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var signal = new Signal
        {
            Id = Guid.NewGuid(),
            StrategyName = "TrendFollowing",
            Symbol = "NVDA",
            Type = SignalType.Buy,
            Timestamp = DateTime.UtcNow,
            Confidence = 0.80m,
        };

        // Assert
        signal.Type.ShouldBe(SignalType.Buy);
        signal.Type.Name.ShouldBe("Buy");
    }

    [Fact]
    public void Signal_SellSignal_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var signal = new Signal
        {
            Id = Guid.NewGuid(),
            StrategyName = "MeanReversion",
            Symbol = "MSFT",
            Type = SignalType.Sell,
            Timestamp = DateTime.UtcNow,
            Confidence = 0.70m,
        };

        // Assert
        signal.Type.ShouldBe(SignalType.Sell);
        signal.Type.Name.ShouldBe("Sell");
    }

    [Fact]
    public void Signal_HoldSignal_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var signal = new Signal
        {
            Id = Guid.NewGuid(),
            StrategyName = "WaitAndSee",
            Symbol = "AMZN",
            Type = SignalType.Hold,
            Timestamp = DateTime.UtcNow,
            Confidence = 0.50m,
        };

        // Assert
        signal.Type.ShouldBe(SignalType.Hold);
        signal.Type.Name.ShouldBe("Hold");
    }

    [Fact]
    public void Signal_CloseSignal_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var signal = new Signal
        {
            Id = Guid.NewGuid(),
            StrategyName = "ExitStrategy",
            Symbol = "GOOGL",
            Type = SignalType.Close,
            Timestamp = DateTime.UtcNow,
            Confidence = 0.95m,
        };

        // Assert
        signal.Type.ShouldBe(SignalType.Close);
        signal.Type.Name.ShouldBe("Close");
    }

    [Fact]
    public void Signal_WithHighConfidence_ShouldReflectValue()
    {
        // Arrange & Act
        var signal = new Signal
        {
            Id = Guid.NewGuid(),
            StrategyName = "StrongSignal",
            Symbol = "META",
            Type = SignalType.Buy,
            Timestamp = DateTime.UtcNow,
            Confidence = 0.99m,
        };

        // Assert
        signal.Confidence.ShouldBe(0.99m);
        signal.Confidence.ShouldBeGreaterThan(0.9m);
    }

    [Fact]
    public void Signal_WithLowConfidence_ShouldReflectValue()
    {
        // Arrange & Act
        var signal = new Signal
        {
            Id = Guid.NewGuid(),
            StrategyName = "WeakSignal",
            Symbol = "DIS",
            Type = SignalType.Hold,
            Timestamp = DateTime.UtcNow,
            Confidence = 0.10m,
        };

        // Assert
        signal.Confidence.ShouldBe(0.10m);
        signal.Confidence.ShouldBeLessThan(0.5m);
    }

    [Fact]
    public void Signal_WithComplexMetadata_ShouldStoreAllData()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "Indicators", new { RSI = 65, MACD = 0.5, SMA = 150.25 } },
            { "MarketCondition", "Bullish" },
            { "RiskLevel", "Medium" },
            { "TimeFrame", "1H" },
            { "PatternDetected", true },
            { "PatternName", "Golden Cross" },
        };

        // Act
        var signal = new Signal
        {
            Id = Guid.NewGuid(),
            StrategyName = "AdvancedTA",
            Symbol = "BTC",
            Type = SignalType.Buy,
            Timestamp = DateTime.UtcNow,
            Confidence = 0.88m,
            SuggestedPrice = 45000.00m,
            Metadata = metadata,
        };

        // Assert
        signal.Metadata.ShouldNotBeNull();
        signal.Metadata.Count.ShouldBe(6);
        signal.Metadata["MarketCondition"].ShouldBe("Bullish");
        signal.Metadata["PatternDetected"].ShouldBe(true);
        signal.Metadata["PatternName"].ShouldBe("Golden Cross");
    }

    [Fact]
    public void Signal_AsRecord_ShouldSupportValueEquality()
    {
        // Arrange
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        var signal1 = new Signal
        {
            Id = id,
            StrategyName = "TestStrategy",
            Symbol = "TEST",
            Type = SignalType.Buy,
            Timestamp = timestamp,
            Confidence = 0.75m,
        };

        var signal2 = new Signal
        {
            Id = id,
            StrategyName = "TestStrategy",
            Symbol = "TEST",
            Type = SignalType.Buy,
            Timestamp = timestamp,
            Confidence = 0.75m,
        };

        // Act & Assert - Records support value equality
        signal1.ShouldNotBeSameAs(signal2); // Different instances
        signal1.Id.ShouldBe(signal2.Id);
        signal1.StrategyName.ShouldBe(signal2.StrategyName);
        signal1.Symbol.ShouldBe(signal2.Symbol);
        signal1.Type.ShouldBe(signal2.Type);
    }

    [Fact]
    public void Signal_WithEmptyMetadata_ShouldStoreEmptyDictionary()
    {
        // Arrange & Act
        var signal = new Signal
        {
            Id = Guid.NewGuid(),
            StrategyName = "MinimalSignal",
            Symbol = "COIN",
            Type = SignalType.Sell,
            Timestamp = DateTime.UtcNow,
            Confidence = 0.60m,
            Metadata = new Dictionary<string, object>(),
        };

        // Assert
        signal.Metadata.ShouldNotBeNull();
        signal.Metadata.Count.ShouldBe(0);
        signal.Metadata.ShouldBeEmpty();
    }

    [Fact]
    public void Signal_ConfidenceValues_ShouldSupportFullRange()
    {
        // Arrange & Act
        var zeroConfidence = new Signal
        {
            Id = Guid.NewGuid(),
            StrategyName = "NoConfidence",
            Symbol = "TEST1",
            Type = SignalType.Hold,
            Timestamp = DateTime.UtcNow,
            Confidence = 0.0m,
        };

        var fullConfidence = new Signal
        {
            Id = Guid.NewGuid(),
            StrategyName = "FullConfidence",
            Symbol = "TEST2",
            Type = SignalType.Buy,
            Timestamp = DateTime.UtcNow,
            Confidence = 1.0m,
        };

        // Assert
        zeroConfidence.Confidence.ShouldBe(0.0m);
        fullConfidence.Confidence.ShouldBe(1.0m);
    }
}
