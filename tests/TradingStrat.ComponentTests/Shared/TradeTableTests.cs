using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Domain.Entities;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the TradeTable component.
/// </summary>
public class TradeTableTests : BunitTestContext
{
    [Fact]
    public void TradeTable_WithNullTrades_RendersEmptyMessage()
    {
        // Arrange & Act
        var cut = Render<TradeTable>(parameters => parameters
            .Add(p => p.Trades, null));

        // Assert
        cut.Markup.ShouldContain("No trades executed");
    }

    [Fact]
    public void TradeTable_WithEmptyTrades_RendersEmptyMessage()
    {
        // Arrange & Act
        var cut = Render<TradeTable>(parameters => parameters
            .Add(p => p.Trades, new List<Trade>()));

        // Assert
        cut.Markup.ShouldContain("No trades executed");
    }

    [Fact]
    public void TradeTable_WithTrades_DisplaysTradeHistory()
    {
        // Arrange
        var trades = new List<Trade>
        {
            new() { DateTime = DateTime.Today, Type = TradeType.Buy, Quantity = 100, Price = 150m, Commission = 5m, Reason = "Buy signal" },
            new() { DateTime = DateTime.Today.AddDays(1), Type = TradeType.Sell, Quantity = 100, Price = 160m, Commission = 5m, Reason = "Sell signal" }
        };

        // Act
        var cut = Render<TradeTable>(parameters => parameters
            .Add(p => p.Trades, trades));

        // Assert
        var table = cut.Find("table");
        table.ShouldNotBeNull();
        cut.Markup.ShouldContain("Buy signal");
        cut.Markup.ShouldContain("Sell signal");
    }

    [Fact]
    public void TradeTable_DisplaysTradeCount()
    {
        // Arrange
        var trades = new List<Trade>
        {
            new() { DateTime = DateTime.Today, Type = TradeType.Buy, Quantity = 100, Price = 150m, Commission = 5m, Reason = "Test" },
            new() { DateTime = DateTime.Today.AddDays(1), Type = TradeType.Sell, Quantity = 100, Price = 160m, Commission = 5m, Reason = "Test" }
        };

        // Act
        var cut = Render<TradeTable>(parameters => parameters
            .Add(p => p.Trades, trades));

        // Assert
        cut.Markup.ShouldContain("Trade History (2 trades)");
    }

    [Fact]
    public void TradeTable_BuyType_DisplaysGreenBadge()
    {
        // Arrange
        var trades = new List<Trade>
        {
            new() { DateTime = DateTime.Today, Type = TradeType.Buy, Quantity = 100, Price = 150m, Commission = 5m, Reason = "Buy signal" }
        };

        // Act
        var cut = Render<TradeTable>(parameters => parameters
            .Add(p => p.Trades, trades));

        // Assert
        var badge = cut.Find("span.bg-green-100");
        badge.ShouldNotBeNull();
        badge.TextContent.ShouldBe("Buy");
    }

    [Fact]
    public void TradeTable_SellType_DisplaysRedBadge()
    {
        // Arrange
        var trades = new List<Trade>
        {
            new() { DateTime = DateTime.Today, Type = TradeType.Sell, Quantity = 100, Price = 150m, Commission = 5m, Reason = "Sell signal" }
        };

        // Act
        var cut = Render<TradeTable>(parameters => parameters
            .Add(p => p.Trades, trades));

        // Assert
        var badge = cut.Find("span.bg-red-100");
        badge.ShouldNotBeNull();
        badge.TextContent.ShouldBe("Sell");
    }

    [Fact]
    public void TradeTable_PositiveProfitLoss_DisplaysWithPositiveStyling()
    {
        // Arrange
        var trades = new List<Trade>
        {
            new() { DateTime = DateTime.Today, Type = TradeType.Buy, Quantity = 100, Price = 150m, Commission = 5m, Reason = "Test" },
            new() { DateTime = DateTime.Today.AddDays(1), Type = TradeType.Sell, Quantity = 100, Price = 160m, Commission = 5m, Reason = "Test", ProfitLoss = 990m }
        };

        // Act
        var cut = Render<TradeTable>(parameters => parameters
            .Add(p => p.Trades, trades));

        // Assert
        var profitCell = cut.FindAll("td.metric-positive");
        profitCell.ShouldNotBeEmpty();
    }

    [Fact]
    public void TradeTable_NegativeProfitLoss_DisplaysWithNegativeStyling()
    {
        // Arrange
        var trades = new List<Trade>
        {
            new() { DateTime = DateTime.Today, Type = TradeType.Buy, Quantity = 100, Price = 160m, Commission = 5m, Reason = "Test" },
            new() { DateTime = DateTime.Today.AddDays(1), Type = TradeType.Sell, Quantity = 100, Price = 150m, Commission = 5m, Reason = "Test", ProfitLoss = -1010m }
        };

        // Act
        var cut = Render<TradeTable>(parameters => parameters
            .Add(p => p.Trades, trades));

        // Assert
        var lossCell = cut.FindAll("td.metric-negative");
        lossCell.ShouldNotBeEmpty();
    }

    [Fact]
    public void TradeTable_WithManyTrades_ShowsToggleButton()
    {
        // Arrange
        var trades = new List<Trade>();
        for (int i = 0; i < 25; i++)
        {
            trades.Add(new() { DateTime = DateTime.Today.AddDays(i), Type = TradeType.Buy, Quantity = 100, Price = 150m, Commission = 5m, Reason = $"Trade {i}" });
        }

        // Act
        var cut = Render<TradeTable>(parameters => parameters
            .Add(p => p.Trades, trades)
            .Add(p => p.MaxDisplayTrades, 20));

        // Assert
        var toggleButton = cut.Find("button");
        toggleButton.ShouldNotBeNull();
        toggleButton.TextContent.ShouldContain("Show All");
    }

    [Fact]
    public void TradeTable_ToggleShowAll_DisplaysAllTrades()
    {
        // Arrange
        var trades = new List<Trade>();
        for (int i = 0; i < 25; i++)
        {
            trades.Add(new() { DateTime = DateTime.Today.AddDays(i), Type = TradeType.Buy, Quantity = 100, Price = 150m, Commission = 5m, Reason = $"Trade {i}" });
        }

        // Act
        var cut = Render<TradeTable>(parameters => parameters
            .Add(p => p.Trades, trades)
            .Add(p => p.MaxDisplayTrades, 20));

        var toggleButton = cut.Find("button");
        toggleButton.Click();

        // Assert
        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(25);

        var showLessButton = cut.Find("button");
        showLessButton.ShouldNotBeNull();
        showLessButton.TextContent.ShouldContain("Show Less");
    }

    [Fact]
    public void TradeTable_LimitsTradesToMaxDisplay()
    {
        // Arrange
        var trades = new List<Trade>();
        for (int i = 0; i < 25; i++)
        {
            trades.Add(new() { DateTime = DateTime.Today.AddDays(i), Type = TradeType.Buy, Quantity = 100, Price = 150m, Commission = 5m, Reason = $"Trade {i}" });
        }

        // Act
        var cut = Render<TradeTable>(parameters => parameters
            .Add(p => p.Trades, trades)
            .Add(p => p.MaxDisplayTrades, 10));

        // Assert
        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(10);
    }

    [Fact]
    public void TradeTable_HasCorrectTableHeaders()
    {
        // Arrange
        var trades = new List<Trade>
        {
            new() { DateTime = DateTime.Today, Type = TradeType.Buy, Quantity = 100, Price = 150m, Commission = 5m, Reason = "Test" }
        };

        // Act
        var cut = Render<TradeTable>(parameters => parameters
            .Add(p => p.Trades, trades));

        // Assert
        cut.Markup.ShouldContain("Date");
        cut.Markup.ShouldContain("Type");
        cut.Markup.ShouldContain("Quantity");
        cut.Markup.ShouldContain("Price");
        cut.Markup.ShouldContain("Commission");
        cut.Markup.ShouldContain("P/L");
        cut.Markup.ShouldContain("Reason");
    }
}
