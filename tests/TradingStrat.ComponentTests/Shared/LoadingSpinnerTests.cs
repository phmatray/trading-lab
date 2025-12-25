using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the LoadingSpinner component.
/// </summary>
public class LoadingSpinnerTests : BunitTestContext
{
    [Fact]
    public void LoadingSpinner_Renders_Successfully()
    {
        // Arrange & Act
        var cut = Render<LoadingSpinner>();

        // Assert
        cut.Markup.ShouldNotBeEmpty();
    }

    [Fact]
    public void LoadingSpinner_HasAccessibilityAttributes()
    {
        // Arrange & Act
        var cut = Render<LoadingSpinner>();

        // Assert
        var container = cut.Find("div[role='status']");
        container.ShouldNotBeNull();
        container.GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void LoadingSpinner_ContainsSpinnerSvg()
    {
        // Arrange & Act
        var cut = Render<LoadingSpinner>();

        // Assert
        var svg = cut.Find("svg");
        svg.ShouldNotBeNull();
        svg.ClassList.ShouldContain("animate-spin");
    }

    [Fact]
    public void LoadingSpinner_HasScreenReaderText()
    {
        // Arrange & Act
        var cut = Render<LoadingSpinner>();

        // Assert
        var srOnly = cut.Find(".sr-only");
        srOnly.ShouldNotBeNull();
        srOnly.TextContent.ShouldBe("Loading...");
    }

    [Fact]
    public void LoadingSpinner_HasCorrectStyling()
    {
        // Arrange & Act
        var cut = Render<LoadingSpinner>();

        // Assert
        var container = cut.Find("div");
        container.ClassList.ShouldContain("flex");
        container.ClassList.ShouldContain("items-center");
        container.ClassList.ShouldContain("justify-center");

        var svg = cut.Find("svg");
        svg.ClassList.ShouldContain("h-8");
        svg.ClassList.ShouldContain("w-8");
        svg.ClassList.ShouldContain("text-trading-blue");
    }
}
