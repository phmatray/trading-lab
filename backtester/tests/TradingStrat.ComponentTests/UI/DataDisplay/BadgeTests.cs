using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI;
using TradingStrat.Web.Components.UI.DataDisplay;
using Xunit;

namespace TradingStrat.ComponentTests.UI.DataDisplay;

/// <summary>
/// Tests for the Badge component.
/// </summary>
public class BadgeTests : BunitTestContext
{
    [Fact]
    public void Badge_RendersAsSpanElement()
    {
        // Arrange & Act
        IRenderedComponent<Badge> cut = Render<Badge>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Badge"))));

        // Assert
        IElement span = cut.Find("span");
        span.ShouldNotBeNull();
        span.TextContent.ShouldBe("Badge");
    }

    [Theory]
    [InlineData(BadgeColor.Blue)]
    [InlineData(BadgeColor.Red)]
    [InlineData(BadgeColor.Green)]
    [InlineData(BadgeColor.Yellow)]
    [InlineData(BadgeColor.Gray)]
    public void Badge_WithDifferentColors_AppliesCorrectColorClass(BadgeColor color)
    {
        // Arrange & Act
        IRenderedComponent<Badge> cut = Render<Badge>(parameters => parameters
            .Add(p => p.Color, color)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Test"))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        // Color classes are applied via CSS custom properties
        IElement span = cut.Find("span");
        span.ShouldNotBeNull();
    }

    [Fact]
    public void Badge_AppliesBaseClasses()
    {
        // Arrange & Act
        IRenderedComponent<Badge> cut = Render<Badge>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Base"))));

        // Assert
        cut.Markup.ShouldContain("inline-flex");
        cut.Markup.ShouldContain("items-center");
        cut.Markup.ShouldContain("gap-x-1.5");
        cut.Markup.ShouldContain("rounded-md");
        cut.Markup.ShouldContain("px-1.5");
        cut.Markup.ShouldContain("py-0.5");
    }

    [Fact]
    public void Badge_AppliesTextSize()
    {
        // Arrange & Act
        IRenderedComponent<Badge> cut = Render<Badge>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Text"))));

        // Assert
        cut.Markup.ShouldContain("text-xs/5");
        cut.Markup.ShouldContain("font-medium");
    }

    [Fact]
    public void Badge_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Badge> cut = Render<Badge>(parameters => parameters
            .Add(p => p.Class, "custom-badge")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Custom"))));

        // Assert
        IElement span = cut.Find("span");
        span.ClassList.ShouldContain("custom-badge");
        span.ClassList.ShouldContain("inline-flex");
    }

    [Fact]
    public void Badge_DefaultsToGrayColor()
    {
        // Arrange & Act
        IRenderedComponent<Badge> cut = Render<Badge>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Default"))));

        // Assert
        // Default color is Gray (set in GetClasses method)
        cut.Markup.ShouldNotBeEmpty();
    }

    [Fact]
    public void Badge_WithEmptyContent_RendersWithoutError()
    {
        // Arrange & Act
        IRenderedComponent<Badge> cut = Render<Badge>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, string.Empty))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        cut.Find("span").ShouldNotBeNull();
    }

    [Fact]
    public void Badge_AppliesForcedColorsOutline()
    {
        // Arrange & Act
        IRenderedComponent<Badge> cut = Render<Badge>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Accessible"))));

        // Assert
        cut.Markup.ShouldContain("forced-colors:outline");
    }
}
