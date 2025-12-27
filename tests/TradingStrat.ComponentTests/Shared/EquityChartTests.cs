using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Domain.Entities;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the EquityChart component.
/// </summary>
public class EquityChartTests : BunitTestContext
{
    [Fact]
    public void EquityChart_WithNullData_DisplaysEmptyState()
    {
        // Arrange & Act
        IRenderedComponent<EquityChart> cut = Render<EquityChart>(parameters => parameters
            .Add(p => p.EquityCurve, null));

        // Assert
        cut.Markup.ShouldContain("No equity curve data available.");
        IElement emptyMessage = cut.Find(".text-gray-500");
        emptyMessage.ShouldNotBeNull();
    }

    [Fact]
    public void EquityChart_WithEmptyList_DisplaysEmptyState()
    {
        // Arrange & Act
        IRenderedComponent<EquityChart> cut = Render<EquityChart>(parameters => parameters
            .Add(p => p.EquityCurve, new List<EquityPoint>()));

        // Assert
        cut.Markup.ShouldContain("No equity curve data available.");
    }

    [Fact]
    public void EquityChart_WithData_DisplaysTitle()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose; // Allow all JS calls for ApexCharts

        var equityCurve = new List<EquityPoint>
        {
            new(DateTime.Today.AddDays(-2), 10000m, 0),
            new(DateTime.Today.AddDays(-1), 10500m, 100),
            new(DateTime.Today, 11000m, 100)
        };

        // Act
        IRenderedComponent<EquityChart> cut = Render<EquityChart>(parameters => parameters
            .Add(p => p.EquityCurve, equityCurve));

        // Assert
        IElement title = cut.Find("h3");
        title.TextContent.ShouldBe("Equity Curve");
    }

    [Fact]
    public void EquityChart_WithData_RendersCard()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        var equityCurve = new List<EquityPoint>
        {
            new(DateTime.Today, 10000m, 0)
        };

        // Act
        IRenderedComponent<EquityChart> cut = Render<EquityChart>(parameters => parameters
            .Add(p => p.EquityCurve, equityCurve));

        // Assert
        IElement card = cut.Find("div.card");
        card.ShouldNotBeNull();
    }

    [Fact]
    public void EquityChart_WithData_DoesNotDisplayEmptyState()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        var equityCurve = new List<EquityPoint>
        {
            new(DateTime.Today, 10000m, 0)
        };

        // Act
        IRenderedComponent<EquityChart> cut = Render<EquityChart>(parameters => parameters
            .Add(p => p.EquityCurve, equityCurve));

        // Assert
        cut.Markup.ShouldNotContain("No equity curve data available.");
    }

    [Fact]
    public void EquityChart_WithSingleDataPoint_RendersSuccessfully()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        var equityCurve = new List<EquityPoint>
        {
            new(DateTime.Today, 5000m, 50)
        };

        // Act
        IRenderedComponent<EquityChart> cut = Render<EquityChart>(parameters => parameters
            .Add(p => p.EquityCurve, equityCurve));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        cut.Find("h3").TextContent.ShouldBe("Equity Curve");
    }

    [Fact]
    public void EquityChart_WithMultipleDataPoints_RendersSuccessfully()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        var equityCurve = new List<EquityPoint>
        {
            new(DateTime.Today.AddDays(-5), 10000m, 0),
            new(DateTime.Today.AddDays(-4), 10200m, 50),
            new(DateTime.Today.AddDays(-3), 10100m, 50),
            new(DateTime.Today.AddDays(-2), 10300m, 50),
            new(DateTime.Today.AddDays(-1), 10500m, 100),
            new(DateTime.Today, 10800m, 100)
        };

        // Act
        IRenderedComponent<EquityChart> cut = Render<EquityChart>(parameters => parameters
            .Add(p => p.EquityCurve, equityCurve));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement title = cut.Find("h3");
        title.TextContent.ShouldBe("Equity Curve");
    }
}
