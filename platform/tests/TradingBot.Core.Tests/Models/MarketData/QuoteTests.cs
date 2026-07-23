// <copyright file="QuoteTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.MarketData;

namespace TradingBot.Core.Tests.Models.MarketData;

/// <summary>
/// Unit tests for the Quote model.
/// </summary>
public sealed class QuoteTests
{
    [Fact]
    public void Quote_WhenCreatedWithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var timestamp = DateTime.UtcNow;
        var quote = new Quote
        {
            Symbol = "SPY",
            Timestamp = timestamp,
            Price = 450.00m,
            Bid = 449.98m,
            Ask = 450.02m,
            Volume = 1000000,
            Change = 2.50m,
            ChangePercent = 0.56m,
        };

        // Assert
        quote.Symbol.ShouldBe("SPY");
        quote.Timestamp.ShouldBe(timestamp);
        quote.Price.ShouldBe(450.00m);
        quote.Bid.ShouldBe(449.98m);
        quote.Ask.ShouldBe(450.02m);
        quote.Volume.ShouldBe(1000000);
        quote.Change.ShouldBe(2.50m);
        quote.ChangePercent.ShouldBe(0.56m);
    }

    [Fact]
    public void Quote_Spread_ShouldBeCalculatedCorrectly()
    {
        // Arrange & Act
        var quote = new Quote
        {
            Symbol = "AAPL",
            Timestamp = DateTime.UtcNow,
            Price = 180.00m,
            Bid = 179.95m,
            Ask = 180.05m,
            Volume = 500000,
            Change = 1.50m,
            ChangePercent = 0.84m,
        };

        // Assert
        quote.Spread.ShouldBe(0.10m); // 180.05 - 179.95 = 0.10
    }

    [Fact]
    public void Quote_MidPrice_ShouldBeCalculatedCorrectly()
    {
        // Arrange & Act
        var quote = new Quote
        {
            Symbol = "TSLA",
            Timestamp = DateTime.UtcNow,
            Price = 250.00m,
            Bid = 249.90m,
            Ask = 250.10m,
            Volume = 750000,
            Change = -3.25m,
            ChangePercent = -1.28m,
        };

        // Assert
        quote.MidPrice.ShouldBe(250.00m); // (249.90 + 250.10) / 2 = 250.00
    }

    [Fact]
    public void Quote_WithPositiveChange_ShouldReflectGain()
    {
        // Arrange & Act
        var quote = new Quote
        {
            Symbol = "NVDA",
            Timestamp = DateTime.UtcNow,
            Price = 500.00m,
            Bid = 499.95m,
            Ask = 500.05m,
            Volume = 300000,
            Change = 15.00m,
            ChangePercent = 3.09m,
        };

        // Assert
        quote.Change.ShouldBeGreaterThan(0);
        quote.ChangePercent.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Quote_WithNegativeChange_ShouldReflectLoss()
    {
        // Arrange & Act
        var quote = new Quote
        {
            Symbol = "MSFT",
            Timestamp = DateTime.UtcNow,
            Price = 350.00m,
            Bid = 349.98m,
            Ask = 350.02m,
            Volume = 200000,
            Change = -5.00m,
            ChangePercent = -1.41m,
        };

        // Assert
        quote.Change.ShouldBeLessThan(0);
        quote.ChangePercent.ShouldBeLessThan(0);
    }

    [Fact]
    public void Quote_WithZeroChange_ShouldBeUnchanged()
    {
        // Arrange & Act
        var quote = new Quote
        {
            Symbol = "GOOGL",
            Timestamp = DateTime.UtcNow,
            Price = 140.00m,
            Bid = 139.98m,
            Ask = 140.02m,
            Volume = 150000,
            Change = 0m,
            ChangePercent = 0m,
        };

        // Assert
        quote.Change.ShouldBe(0);
        quote.ChangePercent.ShouldBe(0);
    }

    [Fact]
    public void Quote_WithTightSpread_ShouldHaveSmallSpread()
    {
        // Arrange & Act
        var quote = new Quote
        {
            Symbol = "SPY",
            Timestamp = DateTime.UtcNow,
            Price = 450.00m,
            Bid = 449.99m,
            Ask = 450.01m,
            Volume = 2000000,
            Change = 0.50m,
            ChangePercent = 0.11m,
        };

        // Assert
        quote.Spread.ShouldBe(0.02m);
        quote.Spread.ShouldBeLessThan(0.05m);
    }

    [Fact]
    public void Quote_WithWideSpread_ShouldHaveLargeSpread()
    {
        // Arrange & Act
        var quote = new Quote
        {
            Symbol = "ILLIQUID",
            Timestamp = DateTime.UtcNow,
            Price = 100.00m,
            Bid = 99.00m,
            Ask = 101.00m,
            Volume = 1000,
            Change = 0m,
            ChangePercent = 0m,
        };

        // Assert
        quote.Spread.ShouldBe(2.00m);
        quote.Spread.ShouldBeGreaterThan(1.00m);
    }

    [Fact]
    public void Quote_MidPrice_ShouldAlwaysBeBetweenBidAndAsk()
    {
        // Arrange & Act
        var quote = new Quote
        {
            Symbol = "AMZN",
            Timestamp = DateTime.UtcNow,
            Price = 150.00m,
            Bid = 149.80m,
            Ask = 150.20m,
            Volume = 400000,
            Change = 2.00m,
            ChangePercent = 1.35m,
        };

        // Assert
        quote.MidPrice.ShouldBeGreaterThanOrEqualTo(quote.Bid);
        quote.MidPrice.ShouldBeLessThanOrEqualTo(quote.Ask);
        quote.MidPrice.ShouldBe(150.00m);
    }

    [Fact]
    public void Quote_AsRecord_ShouldSupportValueEquality()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var quote1 = new Quote
        {
            Symbol = "META",
            Timestamp = timestamp,
            Price = 300.00m,
            Bid = 299.95m,
            Ask = 300.05m,
            Volume = 600000,
            Change = 5.00m,
            ChangePercent = 1.69m,
        };

        var quote2 = new Quote
        {
            Symbol = "META",
            Timestamp = timestamp,
            Price = 300.00m,
            Bid = 299.95m,
            Ask = 300.05m,
            Volume = 600000,
            Change = 5.00m,
            ChangePercent = 1.69m,
        };

        // Act & Assert - Records support value equality
        quote1.ShouldNotBeSameAs(quote2);
        quote1.Equals(quote2).ShouldBeTrue();
    }

    [Fact]
    public void Quote_WithHighVolume_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var quote = new Quote
        {
            Symbol = "SPY",
            Timestamp = DateTime.UtcNow,
            Price = 450.00m,
            Bid = 449.98m,
            Ask = 450.02m,
            Volume = 999_999_999,
            Change = 1.00m,
            ChangePercent = 0.22m,
        };

        // Assert
        quote.Volume.ShouldBe(999_999_999);
    }

    [Fact]
    public void Quote_WithDecimalPrices_ShouldMaintainPrecision()
    {
        // Arrange & Act
        var quote = new Quote
        {
            Symbol = "BTC",
            Timestamp = DateTime.UtcNow,
            Price = 45123.456789m,
            Bid = 45123.45m,
            Ask = 45123.46m,
            Volume = 100,
            Change = 1234.56m,
            ChangePercent = 2.81m,
        };

        // Assert
        quote.Price.ShouldBe(45123.456789m);
        quote.Spread.ShouldBe(0.01m);
    }
}
