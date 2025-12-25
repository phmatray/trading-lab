using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Domain.Entities;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the MetricsGrid component.
/// </summary>
public class MetricsGridTests : BunitTestContext
{
    private static PerformanceMetrics CreateTestMetrics(
        decimal totalReturnPercentage = 0.15m,
        decimal sharpeRatio = 1.5m,
        decimal winRate = 0.65m,
        int totalTrades = 50,
        decimal profitFactor = 2.0m,
        decimal maxDrawdownPercentage = -0.08m)
    {
        return new PerformanceMetrics(
            InitialCapital: 10000m,
            FinalEquity: 10000m * (1 + totalReturnPercentage),
            TotalReturn: 10000m * totalReturnPercentage,
            TotalReturnPercentage: totalReturnPercentage,
            AnnualizedReturn: totalReturnPercentage * 0.5m,
            TotalTrades: totalTrades,
            WinningTrades: (int)(totalTrades * winRate),
            LosingTrades: (int)(totalTrades * (1 - winRate)),
            WinRate: winRate,
            AverageWin: 100m,
            AverageLoss: -50m,
            LargestWin: 200m,
            LargestLoss: -100m,
            ProfitFactor: profitFactor,
            MaxConsecutiveWins: 5,
            MaxConsecutiveLosses: 3,
            MaxDrawdown: 10000m * maxDrawdownPercentage,
            MaxDrawdownPercentage: maxDrawdownPercentage,
            SharpeRatio: sharpeRatio,
            Volatility: 0.15m,
            TotalDays: 365,
            DaysInMarket: 300,
            MarketExposurePercentage: 0.82m
        );
    }

    [Fact]
    public void MetricsGrid_WithNullMetrics_DisplaysNoMetricsMessage()
    {
        // Arrange & Act
        var cut = Render<MetricsGrid>(parameters => parameters
            .Add(p => p.Metrics, null));

        // Assert
        cut.Markup.ShouldContain("No performance metrics available");
    }

    [Fact]
    public void MetricsGrid_WithMetrics_DisplaysAllMetricCards()
    {
        // Arrange
        var metrics = CreateTestMetrics();

        // Act
        var cut = Render<MetricsGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        var metricCards = cut.FindAll("div.card");
        metricCards.Count.ShouldBe(8);
    }

    [Fact]
    public void MetricsGrid_DisplaysTotalReturnLabel()
    {
        // Arrange
        var metrics = CreateTestMetrics(totalReturnPercentage: 0.25m);

        // Act
        var cut = Render<MetricsGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        cut.Markup.ShouldContain("Total Return");
    }

    [Fact]
    public void MetricsGrid_DisplaysSharpeRatioLabel()
    {
        // Arrange
        var metrics = CreateTestMetrics(sharpeRatio: 2.34m);

        // Act
        var cut = Render<MetricsGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        cut.Markup.ShouldContain("Sharpe Ratio");
    }

    [Fact]
    public void MetricsGrid_DisplaysWinRateLabel()
    {
        // Arrange
        var metrics = CreateTestMetrics(winRate: 0.75m);

        // Act
        var cut = Render<MetricsGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        cut.Markup.ShouldContain("Win Rate");
    }

    [Fact]
    public void MetricsGrid_DisplaysTotalTradesLabel()
    {
        // Arrange
        var metrics = CreateTestMetrics(totalTrades: 123);

        // Act
        var cut = Render<MetricsGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        cut.Markup.ShouldContain("Total Trades");
        cut.Markup.ShouldContain("123");
    }

    [Fact]
    public void MetricsGrid_DisplaysProfitFactorLabel()
    {
        // Arrange
        var metrics = CreateTestMetrics(profitFactor: 3.25m);

        // Act
        var cut = Render<MetricsGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        cut.Markup.ShouldContain("Profit Factor");
    }

    [Fact]
    public void MetricsGrid_DisplaysMaxDrawdownLabel()
    {
        // Arrange
        var metrics = CreateTestMetrics(maxDrawdownPercentage: -0.12m);

        // Act
        var cut = Render<MetricsGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert
        cut.Markup.ShouldContain("Max Drawdown");
    }

    [Fact]
    public void MetricsGrid_WithPositiveReturn_ShowsPositiveStyling()
    {
        // Arrange
        var metrics = CreateTestMetrics(totalReturnPercentage: 0.20m);

        // Act
        var cut = Render<MetricsGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert - Total Return metric should have positive styling
        var positiveMetrics = cut.FindAll(".metric-positive");
        positiveMetrics.ShouldNotBeEmpty();
    }

    [Fact]
    public void MetricsGrid_WithNegativeReturn_ShowsNegativeStyling()
    {
        // Arrange
        var metrics = CreateTestMetrics(totalReturnPercentage: -0.10m, sharpeRatio: -0.5m, winRate: 0.45m, profitFactor: 0.8m, maxDrawdownPercentage: -0.15m);

        // Act
        var cut = Render<MetricsGrid>(parameters => parameters
            .Add(p => p.Metrics, metrics));

        // Assert - Total Return metric should have negative styling
        var negativeMetrics = cut.FindAll(".metric-negative");
        negativeMetrics.ShouldNotBeEmpty();
    }
}
