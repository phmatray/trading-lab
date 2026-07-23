using AngleSharp.Dom;
using Bunit;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Strategies;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the StrategyAnalysisPanel component - AI-powered strategy analysis.
/// </summary>
public class StrategyAnalysisPanelTests : BunitTestContext
{
    private readonly IAnalyzeStrategyUseCase _analyzeStrategyUseCase;
    private readonly IStrategyRegistry _strategyRegistry;

    public StrategyAnalysisPanelTests()
    {
        _analyzeStrategyUseCase = A.Fake<IAnalyzeStrategyUseCase>();
        _strategyRegistry = A.Fake<IStrategyRegistry>();

        Services.Add(new ServiceDescriptor(typeof(IAnalyzeStrategyUseCase), _analyzeStrategyUseCase));
        Services.Add(new ServiceDescriptor(typeof(IStrategyRegistry), _strategyRegistry));
    }

    [Fact]
    public void StrategyAnalysisPanel_InitialRender_ShowsAnalyzeButton()
    {
        // Arrange & Act
        IRenderedComponent<StrategyAnalysisPanel> cut = Render<StrategyAnalysisPanel>(parameters => parameters
            .Add(p => p.Ticker, "AAPL")
            .Add(p => p.StrategyType, "rsi"));

        // Assert
        cut.Markup.ShouldContain("AI Strategy Analysis");
        cut.Markup.ShouldContain("Get AI-powered insights and recommendations");

        IElement button = cut.Find("[data-testid='analyze-button']");
        button.ShouldNotBeNull();
        button.TextContent.ShouldContain("Get AI Strategy Analysis");
    }

    [Fact]
    public void StrategyAnalysisPanel_WithRequiredParameters_RendersCorrectly()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { ["Period"] = 14 };

        // Act
        IRenderedComponent<StrategyAnalysisPanel> cut = Render<StrategyAnalysisPanel>(builder => builder
            .Add(p => p.Ticker, "MSFT")
            .Add(p => p.StrategyType, "macd")
            .Add(p => p.StrategyParameters, parameters));

        // Assert
        cut.Markup.ShouldContain("AI Strategy Analysis");
        IElement button = cut.Find("[data-testid='analyze-button']");
        button.ShouldNotBeNull();
    }

    [Fact]
    public void StrategyAnalysisPanel_HasCorrectCardStyling()
    {
        // Arrange & Act
        IRenderedComponent<StrategyAnalysisPanel> cut = Render<StrategyAnalysisPanel>(parameters => parameters
            .Add(p => p.Ticker, "AAPL")
            .Add(p => p.StrategyType, "rsi"));

        // Assert
        IElement card = cut.Find(".card");
        card.ShouldNotBeNull();
    }

    [Fact]
    public void StrategyAnalysisPanel_AnalyzeButton_HasCorrectStyling()
    {
        // Arrange & Act
        IRenderedComponent<StrategyAnalysisPanel> cut = Render<StrategyAnalysisPanel>(parameters => parameters
            .Add(p => p.Ticker, "AAPL")
            .Add(p => p.StrategyType, "rsi"));

        // Assert
        IElement button = cut.Find("[data-testid='analyze-button']");
        button.ClassList.ShouldContain("bg-trading-blue");
        button.ClassList.ShouldContain("text-white");
        button.ClassList.ShouldContain("rounded-lg");
    }
}
