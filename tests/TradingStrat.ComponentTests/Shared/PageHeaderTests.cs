using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the PageHeader component.
/// </summary>
public class PageHeaderTests : BunitTestContext
{
    [Fact]
    public void PageHeader_WithTitle_DisplaysTitle()
    {
        // Arrange
        string title = "Dashboard";

        // Act
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, title));

        // Assert
        var titleElement = cut.Find("h1");
        titleElement.TextContent.ShouldBe(title);
        titleElement.ClassList.ShouldContain("text-3xl");
        titleElement.ClassList.ShouldContain("font-bold");
    }

    [Fact]
    public void PageHeader_WithDescription_DisplaysDescription()
    {
        // Arrange
        string description = "Manage your portfolios and strategies";

        // Act
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Dashboard")
            .Add(p => p.Description, description));

        // Assert
        var descElement = cut.Find("p");
        descElement.TextContent.ShouldBe(description);
        descElement.ClassList.ShouldContain("text-gray-600");
    }

    [Fact]
    public void PageHeader_WithoutDescription_DoesNotRenderDescriptionElement()
    {
        // Arrange & Act
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Dashboard"));

        // Assert
        var paragraphs = cut.FindAll("p");
        paragraphs.ShouldBeEmpty();
    }

    [Fact]
    public void PageHeader_WithActions_RendersActionButtons()
    {
        // Arrange & Act
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Portfolios")
            .Add(p => p.Actions, builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "test-action-btn");
                builder.AddContent(2, "Create New");
                builder.CloseElement();
            }));

        // Assert
        var button = cut.Find("button.test-action-btn");
        button.ShouldNotBeNull();
        button.TextContent.ShouldBe("Create New");
    }

    [Fact]
    public void PageHeader_WithoutActions_DoesNotRenderActionsContainer()
    {
        // Arrange & Act
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Dashboard"));

        // Assert - should only have the header div, not the actions div
        var headerDiv = cut.Find("[data-testid='page-header']");
        headerDiv.ShouldNotBeNull();

        // Should not have action buttons container
        var actionButtons = cut.FindAll("button");
        actionButtons.ShouldBeEmpty();
    }

    [Fact]
    public void PageHeader_HasCorrectTestId()
    {
        // Arrange & Act
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        var container = cut.Find("[data-testid='page-header']");
        container.ShouldNotBeNull();
        container.ClassList.ShouldContain("flex");
        container.ClassList.ShouldContain("items-center");
        container.ClassList.ShouldContain("justify-between");
    }

    [Fact]
    public void PageHeader_WithAllFeatures_RendersCorrectly()
    {
        // Arrange & Act
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Portfolio Details")
            .Add(p => p.Description, "View and manage your portfolio")
            .Add(p => p.Actions, builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddContent(1, "Edit");
                builder.CloseElement();
            }));

        // Assert
        cut.Find("h1").TextContent.ShouldBe("Portfolio Details");
        cut.Find("p").TextContent.ShouldBe("View and manage your portfolio");
        cut.Markup.ShouldContain("Edit");

        // Should have the main container
        var header = cut.Find("[data-testid='page-header']");
        header.ShouldNotBeNull();
    }
}
