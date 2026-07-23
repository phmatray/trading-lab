using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Typography;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Typography;

/// <summary>
/// Tests for the Heading component.
/// </summary>
public class HeadingTests : BunitTestContext
{
    [Theory]
    [InlineData(1, "h1")]
    [InlineData(2, "h2")]
    [InlineData(3, "h3")]
    [InlineData(4, "h4")]
    [InlineData(5, "h5")]
    [InlineData(6, "h6")]
    public void Heading_WithDifferentLevels_RendersCorrectElement(int level, string expectedTag)
    {
        // Arrange & Act
        IRenderedComponent<Heading> cut = Render<Heading>(parameters => parameters
            .Add(p => p.Level, level)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Test"))));

        // Assert
        IElement heading = cut.Find(expectedTag);
        heading.ShouldNotBeNull();
        heading.TextContent.ShouldBe("Test");
    }

    [Fact]
    public void Heading_DefaultsToH1()
    {
        // Arrange & Act
        IRenderedComponent<Heading> cut = Render<Heading>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Default"))));

        // Assert
        IElement heading = cut.Find("h1");
        heading.ShouldNotBeNull();
    }

    [Fact]
    public void Heading_AppliesDarkModeClasses()
    {
        // Arrange & Act
        IRenderedComponent<Heading> cut = Render<Heading>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Dark Mode"))));

        // Assert
        IElement heading = cut.Find("h1");
        cut.Markup.ShouldContain("text-zinc-950");
        cut.Markup.ShouldContain("dark:text-white");
    }

    [Fact]
    public void Heading_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Heading> cut = Render<Heading>(parameters => parameters
            .Add(p => p.Class, "custom-heading")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Custom"))));

        // Assert
        IElement heading = cut.Find("h1");
        heading.ClassList.ShouldContain("custom-heading");
        heading.ClassList.ShouldContain("text-zinc-950");
    }

    [Fact]
    public void Heading_WithEmptyContent_RendersWithoutError()
    {
        // Arrange & Act
        IRenderedComponent<Heading> cut = Render<Heading>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, string.Empty))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        cut.Find("h1").ShouldNotBeNull();
    }
}
