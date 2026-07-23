using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Typography;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Typography;

/// <summary>
/// Tests for the Text component.
/// </summary>
public class TextTests : BunitTestContext
{
    [Fact]
    public void Text_RendersAsParagraphElement()
    {
        // Arrange & Act
        IRenderedComponent<Text> cut = Render<Text>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Test content"))));

        // Assert
        IElement paragraph = cut.Find("p");
        paragraph.ShouldNotBeNull();
        paragraph.TextContent.ShouldBe("Test content");
    }

    [Fact]
    public void Text_AppliesDefaultTextColor()
    {
        // Arrange & Act
        IRenderedComponent<Text> cut = Render<Text>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Text"))));

        // Assert
        cut.Markup.ShouldContain("text-zinc-500");
    }

    [Fact]
    public void Text_AppliesDarkModeColor()
    {
        // Arrange & Act
        IRenderedComponent<Text> cut = Render<Text>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Dark text"))));

        // Assert
        cut.Markup.ShouldContain("dark:text-zinc-400");
    }

    [Fact]
    public void Text_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Text> cut = Render<Text>(parameters => parameters
            .Add(p => p.Class, "custom-text")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Custom"))));

        // Assert
        IElement paragraph = cut.Find("p");
        paragraph.ClassList.ShouldContain("custom-text");
        paragraph.ClassList.ShouldContain("text-zinc-500");
    }

    [Fact]
    public void Text_WithEmptyContent_RendersWithoutError()
    {
        // Arrange & Act
        IRenderedComponent<Text> cut = Render<Text>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, string.Empty))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        cut.Find("p").ShouldNotBeNull();
    }

    [Fact]
    public void Text_WithNullContent_RendersWithoutError()
    {
        // Arrange & Act
        IRenderedComponent<Text> cut = Render<Text>();

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        cut.Find("p").ShouldNotBeNull();
    }
}
