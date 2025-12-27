using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;
using static TradingStrat.Web.Components.Shared.MetricCardGrid;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the MetricCardGrid component.
/// </summary>
public class MetricCardGridTests : BunitTestContext
{
    [Fact]
    public void MetricCardGrid_WithMetrics_RendersCards()
    {
        // Arrange
        var metrics = new List<MetricCardData>
        {
            new() { Label = "Total Value", Value = "$10,000" },
            new() { Label = "Daily Change", Value = "+2.5%" }
        };

        // Act
        IRenderedComponent<MetricCardGrid> cut = Render<MetricCardGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        cut.Markup.ShouldContain("Total Value");
        cut.Markup.ShouldContain("$10,000");
        cut.Markup.ShouldContain("Daily Change");
        cut.Markup.ShouldContain("+2.5%");
    }

    [Fact]
    public void MetricCardGrid_WithEmptyList_RendersEmptyGrid()
    {
        // Arrange & Act
        IRenderedComponent<MetricCardGrid> cut = Render<MetricCardGrid>(parameters => parameters
            .Add(p => p.Metrics, new List<MetricCardData>()));

        // Assert
        IElement container = cut.Find("[data-testid='metric-card-grid']");
        container.ShouldNotBeNull();
        container.ClassList.ShouldContain("grid");
    }

    [Fact]
    public void MetricCardGrid_WithSubtitle_DisplaysSubtitle()
    {
        // Arrange
        var metrics = new List<MetricCardData>
        {
            new() { Label = "Portfolio Value", Value = "$50,000", Subtitle = "As of today" }
        };

        // Act
        IRenderedComponent<MetricCardGrid> cut = Render<MetricCardGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        cut.Markup.ShouldContain("As of today");
    }

    [Fact]
    public void MetricCardGrid_WithoutSubtitle_DoesNotRenderSubtitle()
    {
        // Arrange
        var metrics = new List<MetricCardData>
        {
            new() { Label = "Value", Value = "$1000" }
        };

        // Act
        IRenderedComponent<MetricCardGrid> cut = Render<MetricCardGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        IReadOnlyList<IElement> subtitles = cut.FindAll("p.text-xs");
        subtitles.ShouldBeEmpty();
    }

    [Fact]
    public void MetricCardGrid_WithPositiveValue_AppliesPositiveStyling()
    {
        // Arrange
        var metrics = new List<MetricCardData>
        {
            new() { Label = "Gain", Value = "+5%", IsPositive = true }
        };

        // Act
        IRenderedComponent<MetricCardGrid> cut = Render<MetricCardGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        IElement valueElement = cut.Find("p.metric-positive");
        valueElement.ShouldNotBeNull();
        valueElement.TextContent.ShouldBe("+5%");
    }

    [Fact]
    public void MetricCardGrid_WithNegativeValue_AppliesNegativeStyling()
    {
        // Arrange
        var metrics = new List<MetricCardData>
        {
            new() { Label = "Loss", Value = "-3%", IsPositive = false }
        };

        // Act
        IRenderedComponent<MetricCardGrid> cut = Render<MetricCardGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        IElement valueElement = cut.Find("p.metric-negative");
        valueElement.ShouldNotBeNull();
        valueElement.TextContent.ShouldBe("-3%");
    }

    [Fact]
    public void MetricCardGrid_WithNeutralValue_AppliesNeutralStyling()
    {
        // Arrange
        var metrics = new List<MetricCardData>
        {
            new() { Label = "Count", Value = "42", IsPositive = null }
        };

        // Act
        IRenderedComponent<MetricCardGrid> cut = Render<MetricCardGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        IElement valueElement = cut.Find("p.text-2xl");
        valueElement.ShouldNotBeNull();
        valueElement.ClassList.ShouldContain("text-gray-900");
    }

    [Theory]
    [InlineData("cash")]
    [InlineData("trending-up")]
    [InlineData("trending-down")]
    [InlineData("portfolio")]
    [InlineData("chart")]
    [InlineData("percentage")]
    public void MetricCardGrid_WithDifferentIcons_RendersIcons(string icon)
    {
        // Arrange
        var metrics = new List<MetricCardData>
        {
            new() { Label = "Test", Value = "100", Icon = icon }
        };

        // Act
        IRenderedComponent<MetricCardGrid> cut = Render<MetricCardGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        IElement svg = cut.Find("svg");
        svg.ShouldNotBeNull();
        svg.ClassList.ShouldContain("w-8");
        svg.ClassList.ShouldContain("h-8");
    }

    [Fact]
    public void MetricCardGrid_WithoutIcon_DoesNotRenderIcon()
    {
        // Arrange
        var metrics = new List<MetricCardData>
        {
            new() { Label = "Value", Value = "$100" }
        };

        // Act
        IRenderedComponent<MetricCardGrid> cut = Render<MetricCardGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        IReadOnlyList<IElement> svgs = cut.FindAll("svg");
        svgs.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(2, "md:grid-cols-2")]
    [InlineData(3, "lg:grid-cols-3")]
    [InlineData(4, "lg:grid-cols-4")]
    [InlineData(5, "xl:grid-cols-5")]
    [InlineData(6, "xl:grid-cols-6")]
    public void MetricCardGrid_WithDifferentColumns_AppliesCorrectGridClass(int columns, string expectedClass)
    {
        // Arrange
        var metrics = new List<MetricCardData>
        {
            new() { Label = "Test", Value = "1" }
        };

        // Act
        IRenderedComponent<MetricCardGrid> cut = Render<MetricCardGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics)
            .Add(p => p.Columns, columns));

        // Assert
        IElement grid = cut.Find("[data-testid='metric-card-grid']");
        grid.ClassList.ShouldContain(expectedClass);
    }

    [Fact]
    public void MetricCardGrid_AlwaysHasGridCol1()
    {
        // Arrange
        var metrics = new List<MetricCardData>
        {
            new() { Label = "Test", Value = "1" }
        };

        // Act
        IRenderedComponent<MetricCardGrid> cut = Render<MetricCardGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        IElement grid = cut.Find("[data-testid='metric-card-grid']");
        grid.ClassList.ShouldContain("grid-cols-1");
    }

    [Fact]
    public void MetricCardGrid_WithMultipleMetrics_RendersAllCards()
    {
        // Arrange
        var metrics = new List<MetricCardData>
        {
            new() { Label = "Metric 1", Value = "100", IsPositive = true },
            new() { Label = "Metric 2", Value = "200", IsPositive = false },
            new() { Label = "Metric 3", Value = "300", IsPositive = null },
            new() { Label = "Metric 4", Value = "400", Icon = "chart", Subtitle = "With icon" }
        };

        // Act
        IRenderedComponent<MetricCardGrid> cut = Render<MetricCardGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        cut.Markup.ShouldContain("Metric 1");
        cut.Markup.ShouldContain("Metric 2");
        cut.Markup.ShouldContain("Metric 3");
        cut.Markup.ShouldContain("Metric 4");
        cut.Markup.ShouldContain("With icon");

        IReadOnlyList<IElement> cards = cut.FindAll("div.card");
        cards.Count.ShouldBe(4);
    }
}
