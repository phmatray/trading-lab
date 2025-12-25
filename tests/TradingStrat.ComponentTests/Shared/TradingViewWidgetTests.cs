using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the TradingViewWidget component.
/// </summary>
public class TradingViewWidgetTests : BunitTestContext
{
    [Fact]
    public void TradingViewWidget_WithDefaultParameters_RendersSuccessfully()
    {
        // Arrange & Act
        var cut = Render<TradingViewWidget>();

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        var container = cut.Find("div.tradingview-widget-container");
        container.ShouldNotBeNull();
    }

    [Fact]
    public void TradingViewWidget_WithCustomTicker_UsesTickerInAriaLabel()
    {
        // Arrange
        string ticker = "AAPL";

        // Act
        var cut = Render<TradingViewWidget>(parameters => parameters
            .Add(p => p.Ticker, ticker));

        // Assert
        var container = cut.Find("div[role='region']");
        container.GetAttribute("aria-label").ShouldBe($"Trading chart for {ticker}");
    }

    [Fact]
    public void TradingViewWidget_HasResponsiveHeightClasses()
    {
        // Arrange & Act
        var cut = Render<TradingViewWidget>();

        // Assert
        var container = cut.Find("div.tradingview-widget-container");
        container.ClassList.ShouldContain("h-96");
        container.ClassList.ShouldContain("md:h-[600px]");
    }

    [Fact]
    public void TradingViewWidget_CreatesUniqueWidgetId()
    {
        // Arrange & Act
        var cut = Render<TradingViewWidget>();

        // Assert
        var chartDiv = cut.Find("div[id^='tradingview_chart_']");
        chartDiv.ShouldNotBeNull();
        chartDiv.Id.ShouldStartWith("tradingview_chart_");
    }

    [Theory]
    [InlineData("MSFT")]
    [InlineData("GOOGL")]
    [InlineData("TSLA")]
    public void TradingViewWidget_WithDifferentTickers_RendersCorrectly(string ticker)
    {
        // Arrange & Act
        var cut = Render<TradingViewWidget>(parameters => parameters
            .Add(p => p.Ticker, ticker));

        // Assert
        var container = cut.Find("div[role='region']");
        container.GetAttribute("aria-label")!.ShouldContain(ticker);
    }

    [Theory]
    [InlineData("dark")]
    [InlineData("light")]
    public void TradingViewWidget_WithDifferentThemes_RendersSuccessfully(string theme)
    {
        // Arrange & Act
        var cut = Render<TradingViewWidget>(parameters => parameters
            .Add(p => p.Ticker, "AAPL")
            .Add(p => p.Theme, theme));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        var container = cut.Find("div.tradingview-widget-container");
        container.ShouldNotBeNull();
    }
}
