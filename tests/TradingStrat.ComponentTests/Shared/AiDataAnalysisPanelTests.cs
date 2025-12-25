using Bunit;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using TradingStrat.Web.Services;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the AiDataAnalysisPanel component - AI-powered data analysis.
/// </summary>
public class AiDataAnalysisPanelTests : BunitTestContext
{
    private readonly IDataAnalysisService _dataAnalysisService;

    public AiDataAnalysisPanelTests()
    {
        _dataAnalysisService = A.Fake<IDataAnalysisService>();

        Services.Add(new ServiceDescriptor(typeof(IDataAnalysisService), _dataAnalysisService));
    }

    [Fact]
    public void AiDataAnalysisPanel_InitialRender_ShowsAnalyzeButton()
    {
        // Arrange
        var dataSummary = CreateSampleDataSummary();

        // Act
        var cut = Render<AiDataAnalysisPanel>(parameters => parameters
            .Add(p => p.DataSummary, dataSummary));

        // Assert
        cut.Markup.ShouldContain("Get AI Data Analysis");
        var button = cut.Find("[data-testid='analyze-data-button']");
        button.ShouldNotBeNull();
        button.TextContent.ShouldContain("Get AI Data Analysis");
    }

    [Fact]
    public void AiDataAnalysisPanel_WithRequiredParameter_RendersCorrectly()
    {
        // Arrange
        var dataSummary = CreateSampleDataSummary();

        // Act
        var cut = Render<AiDataAnalysisPanel>(parameters => parameters
            .Add(p => p.DataSummary, dataSummary));

        // Assert
        var panel = cut.Find("[data-testid='ai-analysis-panel']");
        panel.ShouldNotBeNull();
        panel.ClassList.ShouldContain("card");
    }

    [Fact]
    public void AiDataAnalysisPanel_InitialState_DoesNotShowAnalysisResults()
    {
        // Arrange
        var dataSummary = CreateSampleDataSummary();

        // Act
        var cut = Render<AiDataAnalysisPanel>(parameters => parameters
            .Add(p => p.DataSummary, dataSummary));

        // Assert
        cut.Markup.ShouldNotContain("AI ANALYSIS");
        cut.Markup.ShouldNotContain("Summary");
        cut.Markup.ShouldNotContain("Recommendations");
    }

    [Fact]
    public void AiDataAnalysisPanel_InitialState_DoesNotShowProgressIndicator()
    {
        // Arrange
        var dataSummary = CreateSampleDataSummary();

        // Act
        var cut = Render<AiDataAnalysisPanel>(parameters => parameters
            .Add(p => p.DataSummary, dataSummary));

        // Assert
        cut.Markup.ShouldNotContain("Analyzing market data...");
    }

    [Fact]
    public void AiDataAnalysisPanel_AnalyzeButton_HasCorrectStyling()
    {
        // Arrange
        var dataSummary = CreateSampleDataSummary();

        // Act
        var cut = Render<AiDataAnalysisPanel>(parameters => parameters
            .Add(p => p.DataSummary, dataSummary));

        // Assert
        var button = cut.Find("[data-testid='analyze-data-button']");
        button.ClassList.ShouldContain("btn-primary");
    }

    [Fact]
    public void AiDataAnalysisPanel_Panel_HasTestId()
    {
        // Arrange
        var dataSummary = CreateSampleDataSummary();

        // Act
        var cut = Render<AiDataAnalysisPanel>(parameters => parameters
            .Add(p => p.DataSummary, dataSummary));

        // Assert
        var panel = cut.Find("[data-testid='ai-analysis-panel']");
        panel.ShouldNotBeNull();
    }

    [Fact]
    public void AiDataAnalysisPanel_WithDifferentDataSummary_Renders()
    {
        // Arrange
        var dataSummary = new DataSummaryResult(
            Ticker: "MSFT",
            ISIN: "US5949181045",
            TotalRecords: 500,
            NewRecords: 5,
            OldestDate: DateTime.Today.AddYears(-2),
            LatestDate: DateTime.Today,
            MinPrice: 200m,
            MaxPrice: 400m,
            LatestClose: 350m);

        // Act
        var cut = Render<AiDataAnalysisPanel>(parameters => parameters
            .Add(p => p.DataSummary, dataSummary));

        // Assert
        cut.Markup.ShouldContain("Get AI Data Analysis");
    }

    private static DataSummaryResult CreateSampleDataSummary()
    {
        return new DataSummaryResult(
            Ticker: "AAPL",
            ISIN: "US0378331005",
            TotalRecords: 1000,
            NewRecords: 10,
            OldestDate: DateTime.Today.AddYears(-3),
            LatestDate: DateTime.Today,
            MinPrice: 100m,
            MaxPrice: 200m,
            LatestClose: 150m);
    }
}
