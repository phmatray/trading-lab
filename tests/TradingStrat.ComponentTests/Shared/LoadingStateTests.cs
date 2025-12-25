using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;
using static TradingStrat.Web.Components.Shared.LoadingState;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the LoadingState component.
/// </summary>
public class LoadingStateTests : BunitTestContext
{
    [Fact]
    public void LoadingState_WithDefaultStyle_DisplaysSpinner()
    {
        // Arrange & Act
        var cut = Render<LoadingState>();

        // Assert
        var spinner = cut.Find("div.animate-spin");
        spinner.ShouldNotBeNull();
        spinner.ClassList.ShouldContain("rounded-full");
        spinner.ClassList.ShouldContain("border-b-2");
        spinner.ClassList.ShouldContain("border-trading-blue");
    }

    [Fact]
    public void LoadingState_WithSpinnerStyle_DisplaysMessage()
    {
        // Arrange
        string message = "Loading data...";

        // Act
        var cut = Render<LoadingState>(parameters => parameters
            .Add(p => p.Style, LoadingStyle.Spinner)
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.ShouldContain(message);
    }

    [Fact]
    public void LoadingState_WithDefaultMessage_DisplaysLoadingText()
    {
        // Arrange & Act
        var cut = Render<LoadingState>(parameters => parameters
            .Add(p => p.Style, LoadingStyle.Spinner));

        // Assert
        cut.Markup.ShouldContain("Loading...");
    }

    [Fact]
    public void LoadingState_WithSkeletonStyle_DisplaysSkeletonLoader()
    {
        // Arrange & Act
        var cut = Render<LoadingState>(parameters => parameters
            .Add(p => p.Style, LoadingStyle.Skeleton));

        // Assert
        var skeleton = cut.Find("div.animate-pulse");
        skeleton.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("card")]
    [InlineData("table")]
    [InlineData("list")]
    [InlineData("chart")]
    public void LoadingState_WithDifferentSkeletonTypes_RendersCorrectly(string skeletonType)
    {
        // Arrange & Act
        var cut = Render<LoadingState>(parameters => parameters
            .Add(p => p.Style, LoadingStyle.Skeleton)
            .Add(p => p.SkeletonType, skeletonType));

        // Assert
        var skeleton = cut.Find("div.animate-pulse");
        skeleton.ShouldNotBeNull();
        cut.Markup.ShouldNotBeEmpty();
    }

    [Fact]
    public void LoadingState_CardSkeleton_HasCorrectStructure()
    {
        // Arrange & Act
        var cut = Render<LoadingState>(parameters => parameters
            .Add(p => p.Style, LoadingStyle.Skeleton)
            .Add(p => p.SkeletonType, "card"));

        // Assert
        var skeleton = cut.Find("div.animate-pulse");
        skeleton.ShouldNotBeNull();

        // Should have skeleton bars and grid
        var skeletonBars = cut.FindAll("div.bg-gray-200");
        skeletonBars.Count.ShouldBeGreaterThan(0);

        var grid = cut.Find("div.grid-cols-3");
        grid.ShouldNotBeNull();
    }

    [Fact]
    public void LoadingState_TableSkeleton_HasCorrectStructure()
    {
        // Arrange & Act
        var cut = Render<LoadingState>(parameters => parameters
            .Add(p => p.Style, LoadingStyle.Skeleton)
            .Add(p => p.SkeletonType, "table"));

        // Assert
        var skeleton = cut.Find("div.animate-pulse");
        skeleton.ShouldNotBeNull();

        var spacedDiv = cut.Find("div.space-y-3");
        spacedDiv.ShouldNotBeNull();

        // Should have multiple skeleton rows
        var skeletonRows = cut.FindAll("div.h-8.bg-gray-200");
        skeletonRows.Count.ShouldBe(4);
    }

    [Fact]
    public void LoadingState_ListSkeleton_HasCorrectStructure()
    {
        // Arrange & Act
        var cut = Render<LoadingState>(parameters => parameters
            .Add(p => p.Style, LoadingStyle.Skeleton)
            .Add(p => p.SkeletonType, "list"));

        // Assert
        var skeleton = cut.Find("div.animate-pulse");
        skeleton.ShouldNotBeNull();

        var spacedDiv = cut.Find("div.space-y-4");
        spacedDiv.ShouldNotBeNull();

        // Should have list items with flex layout
        var flexItems = cut.FindAll("div.flex.space-x-4");
        flexItems.Count.ShouldBe(4);
    }

    [Fact]
    public void LoadingState_ChartSkeleton_HasCorrectStructure()
    {
        // Arrange & Act
        var cut = Render<LoadingState>(parameters => parameters
            .Add(p => p.Style, LoadingStyle.Skeleton)
            .Add(p => p.SkeletonType, "chart"));

        // Assert
        var skeleton = cut.Find("div.animate-pulse");
        skeleton.ShouldNotBeNull();

        // Should have chart area
        var chartArea = cut.Find("div.h-64.bg-gray-200");
        chartArea.ShouldNotBeNull();

        // Should have grid of metrics
        var grid = cut.Find("div.grid-cols-4");
        grid.ShouldNotBeNull();
    }

    [Fact]
    public void LoadingState_HasCorrectTestId()
    {
        // Arrange & Act
        var cut = Render<LoadingState>();

        // Assert
        var container = cut.Find("[data-testid='loading-state']");
        container.ShouldNotBeNull();
        container.ClassList.ShouldContain("card");
    }

    [Fact]
    public void LoadingState_Skeleton_DoesNotDisplayMessage()
    {
        // Arrange & Act
        var cut = Render<LoadingState>(parameters => parameters
            .Add(p => p.Style, LoadingStyle.Skeleton)
            .Add(p => p.Message, "This should not appear"));

        // Assert
        cut.Markup.ShouldNotContain("This should not appear");
    }
}
