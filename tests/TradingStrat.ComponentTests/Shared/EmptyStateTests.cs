using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the EmptyState component.
/// </summary>
public class EmptyStateTests : BunitTestContext
{
    [Fact]
    public void EmptyState_WithDefaultParameters_RendersSuccessfully()
    {
        // Arrange & Act
        var cut = Render<EmptyState>();

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        var container = cut.Find("[data-testid='empty-state']");
        container.ShouldNotBeNull();
    }

    [Fact]
    public void EmptyState_WithTitle_DisplaysTitle()
    {
        // Arrange
        string title = "No portfolios found";

        // Act
        var cut = Render<EmptyState>(parameters => parameters
            .Add(p => p.Title, title));

        // Assert
        var titleElement = cut.Find("h3");
        titleElement.TextContent.ShouldBe(title);
    }

    [Fact]
    public void EmptyState_WithDescription_DisplaysDescription()
    {
        // Arrange
        string description = "Create your first portfolio to get started";

        // Act
        var cut = Render<EmptyState>(parameters => parameters
            .Add(p => p.Title, "No data")
            .Add(p => p.Description, description));

        // Assert
        var descElement = cut.Find("p");
        descElement.TextContent.ShouldBe(description);
    }

    [Fact]
    public void EmptyState_WithoutDescription_DoesNotRenderDescriptionElement()
    {
        // Arrange & Act
        var cut = Render<EmptyState>(parameters => parameters
            .Add(p => p.Title, "No data"));

        // Assert
        var paragraphs = cut.FindAll("p");
        paragraphs.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("small", "h-8", "w-8")]
    [InlineData("medium", "h-12", "w-12")]
    [InlineData("large", "h-16", "w-16")]
    public void EmptyState_WithDifferentSizes_AppliesCorrectClasses(string size, string heightClass, string widthClass)
    {
        // Arrange & Act
        var cut = Render<EmptyState>(parameters => parameters
            .Add(p => p.Size, size));

        // Assert
        var svg = cut.Find("svg");
        svg.ClassList.ShouldContain(heightClass);
        svg.ClassList.ShouldContain(widthClass);
    }

    [Theory]
    [InlineData("folder")]
    [InlineData("document")]
    [InlineData("chart")]
    [InlineData("portfolio")]
    [InlineData("alert")]
    [InlineData("search")]
    [InlineData("database")]
    public void EmptyState_WithDifferentIcons_RendersSuccessfully(string icon)
    {
        // Arrange & Act
        var cut = Render<EmptyState>(parameters => parameters
            .Add(p => p.Icon, icon));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        var svg = cut.Find("svg");
        svg.ShouldNotBeNull();

        var path = cut.Find("svg path");
        path.ShouldNotBeNull();
        path.GetAttribute("d").ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void EmptyState_WithActions_RendersActionContent()
    {
        // Arrange & Act
        var cut = Render<EmptyState>(parameters => parameters
            .Add(p => p.Title, "No data")
            .Add(p => p.Actions, builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "test-button");
                builder.AddContent(2, "Create New");
                builder.CloseElement();
            }));

        // Assert
        var button = cut.Find("button.test-button");
        button.ShouldNotBeNull();
        button.TextContent.ShouldBe("Create New");
    }

    [Fact]
    public void EmptyState_HasCorrectStructure()
    {
        // Arrange & Act
        var cut = Render<EmptyState>(parameters => parameters
            .Add(p => p.Title, "Test Title")
            .Add(p => p.Icon, "chart"));

        // Assert
        var container = cut.Find("div.card");
        container.ShouldNotBeNull();

        var svg = cut.Find("svg");
        svg.ShouldNotBeNull();
        svg.ClassList.ShouldContain("text-gray-400");

        var title = cut.Find("h3");
        title.ShouldNotBeNull();
        title.ClassList.ShouldContain("text-sm");
        title.ClassList.ShouldContain("font-medium");
    }
}
