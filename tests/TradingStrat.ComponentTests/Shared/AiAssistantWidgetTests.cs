using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using TradingStrat.Web.Services.State;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the AiAssistantWidget component - AI chat assistant.
/// </summary>
public class AiAssistantWidgetTests : BunitTestContext
{
    private readonly ChatStateService _chatStateService;

    public AiAssistantWidgetTests()
    {
        _chatStateService = new ChatStateService(FakeLocalStorage);
        Services.Add(new ServiceDescriptor(typeof(ChatStateService), _chatStateService));
    }

    [Fact]
    public void AiAssistantWidget_InitialRender_ShowsMinimizedButton()
    {
        // Arrange & Act
        IRenderedComponent<AiAssistantWidget> cut = Render<AiAssistantWidget>();

        // Assert
        cut.Markup.ShouldContain("AI Assistant");
    }

    [Fact]
    public void AiAssistantWidget_MinimizedButton_HasCorrectStyling()
    {
        // Arrange & Act
        IRenderedComponent<AiAssistantWidget> cut = Render<AiAssistantWidget>();

        // Assert
        cut.Markup.ShouldContain("bg-trading-blue");
        cut.Markup.ShouldContain("rounded-full");
        cut.Markup.ShouldContain("shadow-lg");
    }

    [Fact]
    public void AiAssistantWidget_WithCurrentTicker_Renders()
    {
        // Arrange & Act
        IRenderedComponent<AiAssistantWidget> cut = Render<AiAssistantWidget>(parameters => parameters
            .Add(p => p.CurrentTicker, "AAPL"));

        // Assert
        cut.Markup.ShouldContain("AI Assistant");
    }

    [Fact]
    public void AiAssistantWidget_WithoutCurrentTicker_Renders()
    {
        // Arrange & Act
        IRenderedComponent<AiAssistantWidget> cut = Render<AiAssistantWidget>();

        // Assert
        cut.Markup.ShouldContain("AI Assistant");
    }

    [Fact]
    public void AiAssistantWidget_MinimizedState_ShowsIcon()
    {
        // Arrange & Act
        IRenderedComponent<AiAssistantWidget> cut = Render<AiAssistantWidget>();

        // Assert
        // Check for SVG icon
        cut.Markup.ShouldContain("<svg");
        cut.Markup.ShouldContain("viewBox=\"0 0 24 24\"");
    }
}
