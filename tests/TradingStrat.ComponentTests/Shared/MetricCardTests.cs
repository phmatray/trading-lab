using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the MetricCard component.
/// </summary>
public class MetricCardTests : BunitTestContext
{
    [Fact]
    public void MetricCard_WithoutParameters_RendersSuccessfully()
    {
        // Arrange & Act
        IRenderedComponent<MetricCard> cut = Render<MetricCard>();

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        cut.Find("div.card").ShouldNotBeNull();
    }

    [Fact]
    public void MetricCard_WithLabelAndValue_DisplaysCorrectly()
    {
        // Arrange
        string label = "Total Return";
        string value = "+15.50%";

        // Act
        IRenderedComponent<MetricCard> cut = Render<MetricCard>(parameters => parameters
            .Add(p => p.Label, label)
            .Add(p => p.Value, value));

        // Assert
        IElement labelElement = cut.Find(".text-sm");
        IElement valueElement = cut.Find(".text-2xl");

        labelElement.TextContent.ShouldBe(label);
        valueElement.TextContent.ShouldBe(value);
    }

    [Fact]
    public void MetricCard_WithPositiveValue_AppliesPositiveColorClass()
    {
        // Arrange & Act
        IRenderedComponent<MetricCard> cut = Render<MetricCard>(parameters => parameters
            .Add(p => p.Label, "Gain")
            .Add(p => p.Value, "+10%")
            .Add(p => p.IsPositive, true));

        // Assert
        IElement valueElement = cut.Find(".text-2xl");
        valueElement.ClassList.ShouldContain("metric-positive");
        valueElement.ClassList.ShouldNotContain("metric-negative");
    }

    [Fact]
    public void MetricCard_WithNegativeValue_AppliesNegativeColorClass()
    {
        // Arrange & Act
        IRenderedComponent<MetricCard> cut = Render<MetricCard>(parameters => parameters
            .Add(p => p.Label, "Loss")
            .Add(p => p.Value, "-5.25%")
            .Add(p => p.IsPositive, false));

        // Assert
        IElement valueElement = cut.Find(".text-2xl");
        valueElement.ClassList.ShouldContain("metric-negative");
        valueElement.ClassList.ShouldNotContain("metric-positive");
    }

    [Fact]
    public void MetricCard_WithNullIsPositive_AppliesNeutralColorClass()
    {
        // Arrange & Act
        IRenderedComponent<MetricCard> cut = Render<MetricCard>(parameters => parameters
            .Add(p => p.Label, "Neutral")
            .Add(p => p.Value, "0.00%")
            .Add(p => p.IsPositive, null));

        // Assert
        IElement valueElement = cut.Find(".text-2xl");
        valueElement.ClassList.ShouldContain("text-gray-900");
        valueElement.ClassList.ShouldNotContain("metric-positive");
        valueElement.ClassList.ShouldNotContain("metric-negative");
    }

    [Theory]
    [InlineData(true, "metric-positive")]
    [InlineData(false, "metric-negative")]
    public void MetricCard_WithDifferentIsPositiveValues_AppliesCorrectClass(
        bool isPositive,
        string expectedClass)
    {
        // Arrange & Act
        IRenderedComponent<MetricCard> cut = Render<MetricCard>(parameters => parameters
            .Add(p => p.Label, "Test")
            .Add(p => p.Value, "Test")
            .Add(p => p.IsPositive, isPositive));

        // Assert
        cut.Markup.ShouldContain(expectedClass);
    }

    [Fact]
    public void MetricCard_WithEmptyLabel_RendersWithoutError()
    {
        // Arrange & Act
        IRenderedComponent<MetricCard> cut = Render<MetricCard>(parameters => parameters
            .Add(p => p.Label, string.Empty)
            .Add(p => p.Value, "100"));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement labelElement = cut.Find(".text-sm");
        labelElement.TextContent.ShouldBe(string.Empty);
    }

    [Fact]
    public void MetricCard_WithEmptyValue_RendersWithoutError()
    {
        // Arrange & Act
        IRenderedComponent<MetricCard> cut = Render<MetricCard>(parameters => parameters
            .Add(p => p.Label, "Label")
            .Add(p => p.Value, string.Empty));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement valueElement = cut.Find(".text-2xl");
        valueElement.TextContent.ShouldBe(string.Empty);
    }
}
