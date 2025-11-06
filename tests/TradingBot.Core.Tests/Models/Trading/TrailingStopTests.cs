// <copyright file="TrailingStopTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Shouldly;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Core.Tests.Models.Trading;

/// <summary>
/// Unit tests for the TrailingStop model.
/// </summary>
public sealed class TrailingStopTests
{
    [Fact]
    public void TrailingStop_WhenCreatedWithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var positionId = Guid.NewGuid();
        var stopOrderId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var lastUpdated = DateTime.UtcNow;

        var trailingStop = new TrailingStop
        {
            PositionId = positionId,
            StopOrderId = stopOrderId,
            TrailingPercent = 5.0m,
            CurrentStopPrice = 450.00m,
            HighestPrice = 475.00m,
            LowestPrice = 0m,
            IsLong = true,
            CreatedAt = createdAt,
            LastUpdated = lastUpdated,
        };

        // Assert
        trailingStop.PositionId.ShouldBe(positionId);
        trailingStop.StopOrderId.ShouldBe(stopOrderId);
        trailingStop.TrailingPercent.ShouldBe(5.0m);
        trailingStop.CurrentStopPrice.ShouldBe(450.00m);
        trailingStop.HighestPrice.ShouldBe(475.00m);
        trailingStop.LowestPrice.ShouldBe(0m);
        trailingStop.IsLong.ShouldBeTrue();
        trailingStop.CreatedAt.ShouldBe(createdAt);
        trailingStop.LastUpdated.ShouldBe(lastUpdated);
    }

    [Fact]
    public void TrailingStop_LongPosition_ShouldHaveIsLongTrue()
    {
        // Arrange & Act
        var trailingStop = new TrailingStop
        {
            PositionId = Guid.NewGuid(),
            StopOrderId = Guid.NewGuid(),
            TrailingPercent = 3.0m,
            CurrentStopPrice = 180.00m,
            HighestPrice = 185.00m,
            IsLong = true,
        };

        // Assert
        trailingStop.IsLong.ShouldBeTrue();
    }

    [Fact]
    public void TrailingStop_ShortPosition_ShouldHaveIsLongFalse()
    {
        // Arrange & Act
        var trailingStop = new TrailingStop
        {
            PositionId = Guid.NewGuid(),
            StopOrderId = Guid.NewGuid(),
            TrailingPercent = 3.0m,
            CurrentStopPrice = 250.00m,
            LowestPrice = 245.00m,
            IsLong = false,
        };

        // Assert
        trailingStop.IsLong.ShouldBeFalse();
    }

    [Fact]
    public void TrailingStop_DefaultIsLong_ShouldBeTrue()
    {
        // Arrange & Act
        var trailingStop = new TrailingStop
        {
            PositionId = Guid.NewGuid(),
            StopOrderId = Guid.NewGuid(),
            TrailingPercent = 2.5m,
            CurrentStopPrice = 100.00m,
        };

        // Assert
        trailingStop.IsLong.ShouldBeTrue(); // Default value
    }

    [Fact]
    public void TrailingStop_WithSmallTrailingPercent_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var trailingStop = new TrailingStop
        {
            PositionId = Guid.NewGuid(),
            StopOrderId = Guid.NewGuid(),
            TrailingPercent = 0.5m,
            CurrentStopPrice = 500.00m,
        };

        // Assert
        trailingStop.TrailingPercent.ShouldBe(0.5m);
    }

    [Fact]
    public void TrailingStop_WithLargeTrailingPercent_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var trailingStop = new TrailingStop
        {
            PositionId = Guid.NewGuid(),
            StopOrderId = Guid.NewGuid(),
            TrailingPercent = 15.0m,
            CurrentStopPrice = 300.00m,
        };

        // Assert
        trailingStop.TrailingPercent.ShouldBe(15.0m);
    }

    [Fact]
    public void TrailingStop_HighestPrice_ShouldTrackPeakForLongPosition()
    {
        // Arrange & Act
        var trailingStop = new TrailingStop
        {
            PositionId = Guid.NewGuid(),
            StopOrderId = Guid.NewGuid(),
            TrailingPercent = 5.0m,
            CurrentStopPrice = 450.00m,
            HighestPrice = 475.00m,
            IsLong = true,
        };

        // Assert
        trailingStop.HighestPrice.ShouldBe(475.00m);
        trailingStop.HighestPrice.ShouldBeGreaterThan(trailingStop.CurrentStopPrice);
    }

    [Fact]
    public void TrailingStop_LowestPrice_ShouldTrackBottomForShortPosition()
    {
        // Arrange & Act
        var trailingStop = new TrailingStop
        {
            PositionId = Guid.NewGuid(),
            StopOrderId = Guid.NewGuid(),
            TrailingPercent = 5.0m,
            CurrentStopPrice = 250.00m,
            LowestPrice = 235.00m,
            IsLong = false,
        };

        // Assert
        trailingStop.LowestPrice.ShouldBe(235.00m);
        trailingStop.LowestPrice.ShouldBeLessThan(trailingStop.CurrentStopPrice);
    }

    [Fact]
    public void TrailingStop_CreatedAt_ShouldBeSetToUtcNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var trailingStop = new TrailingStop
        {
            PositionId = Guid.NewGuid(),
            StopOrderId = Guid.NewGuid(),
            TrailingPercent = 5.0m,
            CurrentStopPrice = 450.00m,
        };

        var afterCreation = DateTime.UtcNow;

        // Assert
        trailingStop.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
        trailingStop.CreatedAt.ShouldBeLessThanOrEqualTo(afterCreation);
    }

    [Fact]
    public void TrailingStop_LastUpdated_ShouldBeSetToUtcNow()
    {
        // Arrange
        var beforeUpdate = DateTime.UtcNow;

        // Act
        var trailingStop = new TrailingStop
        {
            PositionId = Guid.NewGuid(),
            StopOrderId = Guid.NewGuid(),
            TrailingPercent = 5.0m,
            CurrentStopPrice = 450.00m,
        };

        var afterUpdate = DateTime.UtcNow;

        // Assert
        trailingStop.LastUpdated.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
        trailingStop.LastUpdated.ShouldBeLessThanOrEqualTo(afterUpdate);
    }

    [Fact]
    public void TrailingStop_WhenUpdated_LastUpdatedCanBeChanged()
    {
        // Arrange
        var initialTime = DateTime.UtcNow.AddHours(-1);
        var trailingStop = new TrailingStop
        {
            PositionId = Guid.NewGuid(),
            StopOrderId = Guid.NewGuid(),
            TrailingPercent = 5.0m,
            CurrentStopPrice = 450.00m,
            LastUpdated = initialTime,
        };

        // Act
        var newTime = DateTime.UtcNow;
        trailingStop.LastUpdated = newTime;

        // Assert
        trailingStop.LastUpdated.ShouldBe(newTime);
        trailingStop.LastUpdated.ShouldNotBe(initialTime);
    }

    [Fact]
    public void TrailingStop_WithZeroLowestAndHighestPrices_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var trailingStop = new TrailingStop
        {
            PositionId = Guid.NewGuid(),
            StopOrderId = Guid.NewGuid(),
            TrailingPercent = 5.0m,
            CurrentStopPrice = 450.00m,
            HighestPrice = 0m,
            LowestPrice = 0m,
        };

        // Assert
        trailingStop.HighestPrice.ShouldBe(0m);
        trailingStop.LowestPrice.ShouldBe(0m);
    }
}
