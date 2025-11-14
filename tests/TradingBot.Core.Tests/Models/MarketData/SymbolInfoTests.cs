// <copyright file="SymbolInfoTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Shouldly;
using TradingBot.Core.Models.MarketData;

namespace TradingBot.Core.Tests.Models.MarketData;

/// <summary>
/// Unit tests for the SymbolInfo model.
/// </summary>
public sealed class SymbolInfoTests
{
    [Fact]
    public void SymbolInfo_WhenCreatedWithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var symbolInfo = new SymbolInfo
        {
            Symbol = "AAPL",
            Name = "Apple Inc.",
            Exchange = "NASDAQ",
            AssetType = "Stock",
            Currency = "USD",
            TickSize = 0.01m,
            LotSize = 1m,
            IsTradable = true,
        };

        // Assert
        symbolInfo.Symbol.ShouldBe("AAPL");
        symbolInfo.Name.ShouldBe("Apple Inc.");
        symbolInfo.Exchange.ShouldBe("NASDAQ");
        symbolInfo.AssetType.ShouldBe("Stock");
        symbolInfo.Currency.ShouldBe("USD");
        symbolInfo.TickSize.ShouldBe(0.01m);
        symbolInfo.LotSize.ShouldBe(1m);
        symbolInfo.IsTradable.ShouldBeTrue();
    }

    [Fact]
    public void SymbolInfo_Stock_ShouldHaveCorrectAssetType()
    {
        // Arrange & Act
        var stock = new SymbolInfo
        {
            Symbol = "TSLA",
            Name = "Tesla, Inc.",
            Exchange = "NASDAQ",
            AssetType = "Stock",
            Currency = "USD",
            TickSize = 0.01m,
            LotSize = 1m,
            IsTradable = true,
        };

        // Assert
        stock.AssetType.ShouldBe("Stock");
    }

    [Fact]
    public void SymbolInfo_ETF_ShouldHaveCorrectAssetType()
    {
        // Arrange & Act
        var etf = new SymbolInfo
        {
            Symbol = "SPY",
            Name = "SPDR S&P 500 ETF Trust",
            Exchange = "NYSE",
            AssetType = "ETF",
            Currency = "USD",
            TickSize = 0.01m,
            LotSize = 1m,
            IsTradable = true,
        };

        // Assert
        etf.AssetType.ShouldBe("ETF");
    }

    [Fact]
    public void SymbolInfo_Crypto_ShouldHaveCorrectConfiguration()
    {
        // Arrange & Act
        var crypto = new SymbolInfo
        {
            Symbol = "BTC",
            Name = "Bitcoin",
            Exchange = "Coinbase",
            AssetType = "Crypto",
            Currency = "USD",
            TickSize = 0.01m,
            LotSize = 0.00000001m,
            IsTradable = true,
        };

        // Assert
        crypto.AssetType.ShouldBe("Crypto");
        crypto.LotSize.ShouldBe(0.00000001m); // Satoshi
    }

    [Fact]
    public void SymbolInfo_Forex_ShouldHaveCorrectConfiguration()
    {
        // Arrange & Act
        var forex = new SymbolInfo
        {
            Symbol = "EURUSD",
            Name = "Euro/US Dollar",
            Exchange = "Forex",
            AssetType = "Forex",
            Currency = "USD",
            TickSize = 0.00001m,
            LotSize = 0.01m,
            IsTradable = true,
        };

        // Assert
        forex.AssetType.ShouldBe("Forex");
        forex.TickSize.ShouldBe(0.00001m); // Pip
    }

    [Fact]
    public void SymbolInfo_WhenNotTradable_ShouldHaveIsTradableFalse()
    {
        // Arrange & Act
        var symbolInfo = new SymbolInfo
        {
            Symbol = "DELISTED",
            Name = "Delisted Company",
            Exchange = "NASDAQ",
            AssetType = "Stock",
            Currency = "USD",
            TickSize = 0.01m,
            LotSize = 1m,
            IsTradable = false,
        };

        // Assert
        symbolInfo.IsTradable.ShouldBeFalse();
    }

    [Fact]
    public void SymbolInfo_WithDifferentCurrencies_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var usd = new SymbolInfo
        {
            Symbol = "AAPL",
            Name = "Apple Inc.",
            Exchange = "NASDAQ",
            AssetType = "Stock",
            Currency = "USD",
            TickSize = 0.01m,
            LotSize = 1m,
            IsTradable = true,
        };

        var gbp = new SymbolInfo
        {
            Symbol = "VOD.L",
            Name = "Vodafone Group PLC",
            Exchange = "LSE",
            AssetType = "Stock",
            Currency = "GBP",
            TickSize = 0.01m,
            LotSize = 1m,
            IsTradable = true,
        };

        // Assert
        usd.Currency.ShouldBe("USD");
        gbp.Currency.ShouldBe("GBP");
    }

    [Fact]
    public void SymbolInfo_WithSmallTickSize_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var symbolInfo = new SymbolInfo
        {
            Symbol = "BTCUSD",
            Name = "Bitcoin/USD",
            Exchange = "Crypto Exchange",
            AssetType = "Crypto",
            Currency = "USD",
            TickSize = 0.000001m,
            LotSize = 0.00000001m,
            IsTradable = true,
        };

        // Assert
        symbolInfo.TickSize.ShouldBe(0.000001m);
    }

    [Fact]
    public void SymbolInfo_WithLargeLotSize_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var symbolInfo = new SymbolInfo
        {
            Symbol = "FUTURES",
            Name = "Futures Contract",
            Exchange = "CME",
            AssetType = "Futures",
            Currency = "USD",
            TickSize = 0.25m,
            LotSize = 100m,
            IsTradable = true,
        };

        // Assert
        symbolInfo.LotSize.ShouldBe(100m);
    }

    [Fact]
    public void SymbolInfo_AsRecord_ShouldSupportValueEquality()
    {
        // Arrange
        var symbol1 = new SymbolInfo
        {
            Symbol = "MSFT",
            Name = "Microsoft Corporation",
            Exchange = "NASDAQ",
            AssetType = "Stock",
            Currency = "USD",
            TickSize = 0.01m,
            LotSize = 1m,
            IsTradable = true,
        };

        var symbol2 = new SymbolInfo
        {
            Symbol = "MSFT",
            Name = "Microsoft Corporation",
            Exchange = "NASDAQ",
            AssetType = "Stock",
            Currency = "USD",
            TickSize = 0.01m,
            LotSize = 1m,
            IsTradable = true,
        };

        // Act & Assert - Records support value equality
        symbol1.ShouldNotBeSameAs(symbol2);
        symbol1.Equals(symbol2).ShouldBeTrue();
    }

    [Fact]
    public void SymbolInfo_WithZeroTickSize_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var symbolInfo = new SymbolInfo
        {
            Symbol = "TEST",
            Name = "Test Symbol",
            Exchange = "TEST",
            AssetType = "Test",
            Currency = "USD",
            TickSize = 0m,
            LotSize = 0m,
            IsTradable = false,
        };

        // Assert
        symbolInfo.TickSize.ShouldBe(0m);
        symbolInfo.LotSize.ShouldBe(0m);
    }

    [Fact]
    public void SymbolInfo_WithDifferentExchanges_ShouldDistinguishSymbols()
    {
        // Arrange & Act
        var nyse = new SymbolInfo
        {
            Symbol = "GM",
            Name = "General Motors",
            Exchange = "NYSE",
            AssetType = "Stock",
            Currency = "USD",
            TickSize = 0.01m,
            LotSize = 1m,
            IsTradable = true,
        };

        var nasdaq = new SymbolInfo
        {
            Symbol = "GOOGL",
            Name = "Alphabet Inc.",
            Exchange = "NASDAQ",
            AssetType = "Stock",
            Currency = "USD",
            TickSize = 0.01m,
            LotSize = 1m,
            IsTradable = true,
        };

        // Assert
        nyse.Exchange.ShouldBe("NYSE");
        nasdaq.Exchange.ShouldBe("NASDAQ");
        nyse.Equals(nasdaq).ShouldBeFalse();
    }
}
