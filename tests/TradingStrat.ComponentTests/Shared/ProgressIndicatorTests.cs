using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the ProgressIndicator component.
/// </summary>
public class ProgressIndicatorTests : BunitTestContext
{
    [Fact]
    public void ProgressIndicator_WhenNotVisible_RendersNothing()
    {
        // Arrange & Act
        var cut = Render<ProgressIndicator>(parameters => parameters
            .Add(p => p.IsVisible, false));

        // Assert
        cut.Markup.ShouldBeEmpty();
    }

    [Fact]
    public void ProgressIndicator_WhenVisible_RendersContainer()
    {
        // Arrange & Act
        var cut = Render<ProgressIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.CurrentMessage, "Loading data..."));

        // Assert
        var container = cut.Find("[data-testid='progress-indicator']");
        container.ShouldNotBeNull();
    }

    [Fact]
    public void ProgressIndicator_DisplaysCurrentMessage()
    {
        // Arrange
        string message = "Fetching historical data...";

        // Act
        var cut = Render<ProgressIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.CurrentMessage, message));

        // Assert
        var messageElement = cut.Find(".text-sm.font-medium");
        messageElement.TextContent.ShouldBe(message);
    }

    [Fact]
    public void ProgressIndicator_WithProgress_DisplaysProgressBar()
    {
        // Arrange & Act
        var cut = Render<ProgressIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.CurrentMessage, "Processing...")
            .Add(p => p.Progress, 75));

        // Assert
        var progressBar = cut.Find(".bg-trading-blue");
        progressBar.ShouldNotBeNull();
        progressBar.GetAttribute("style")!.ShouldContain("width: 75%");

        var progressText = cut.Find(".text-xs");
        progressText.TextContent.ShouldBe("75%");
    }

    [Fact]
    public void ProgressIndicator_WithoutProgress_DoesNotDisplayProgressBar()
    {
        // Arrange & Act
        var cut = Render<ProgressIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.CurrentMessage, "Processing...")
            .Add(p => p.Progress, null));

        // Assert
        var progressBars = cut.FindAll(".bg-trading-blue");
        progressBars.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void ProgressIndicator_WithDifferentProgressValues_DisplaysCorrectly(int progress)
    {
        // Arrange & Act
        var cut = Render<ProgressIndicator>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.CurrentMessage, "Processing...")
            .Add(p => p.Progress, progress));

        // Assert
        var progressBar = cut.Find(".bg-trading-blue");
        progressBar.GetAttribute("style")!.ShouldContain($"width: {progress}%");

        var progressText = cut.Find(".text-xs");
        progressText.TextContent.ShouldBe($"{progress}%");
    }
}
