using Shouldly;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.ValueObjects;

public class ComparisonRankingTests
{
    [Fact]
    public void CalculateRanking_WhenVariantAHasHigherSharpe_ShouldRankAHigher()
    {
        // Arrange
        var metricsA = CreateMetrics(sharpeRatio: 2.5m, annualizedReturn: 15m);
        var metricsB = CreateMetrics(sharpeRatio: 1.5m, annualizedReturn: 15m);

        // Act
        var ranking = ComparisonRanking.CalculateRanking(metricsA, metricsB);

        // Assert
        ranking.WinnerIndex.ShouldBe(1);
        ranking.VariantAScore.ShouldBeGreaterThan(ranking.VariantBScore);
    }

    [Fact]
    public void CalculateRanking_WhenVariantBHasHigherSharpe_ShouldRankBHigher()
    {
        // Arrange
        var metricsA = CreateMetrics(sharpeRatio: 1.0m, annualizedReturn: 15m);
        var metricsB = CreateMetrics(sharpeRatio: 2.0m, annualizedReturn: 15m);

        // Act
        var ranking = ComparisonRanking.CalculateRanking(metricsA, metricsB);

        // Assert
        ranking.WinnerIndex.ShouldBe(2);
        ranking.VariantBScore.ShouldBeGreaterThan(ranking.VariantAScore);
    }

    [Fact]
    public void CalculateRanking_WhenScoresAreClose_ShouldReturnTie()
    {
        // Arrange - Create metrics where scores are exactly equal (perfect tie)
        // A wins Sharpe (0.40) + Win Rate (0.10) = 0.50
        // B wins Annualized Return (0.30) + Max Drawdown (0.20) = 0.50
        var metricsA = CreateMetrics(sharpeRatio: 2.0m, annualizedReturn: 10m, maxDrawdownPercentage: -20m, winRate: 60m);
        var metricsB = CreateMetrics(sharpeRatio: 1.5m, annualizedReturn: 15m, maxDrawdownPercentage: -15m, winRate: 55m);

        // Act
        var ranking = ComparisonRanking.CalculateRanking(metricsA, metricsB);

        // Assert - Scores are equal, so it's a tie
        ranking.VariantAScore.ShouldBe(0.50m);
        ranking.VariantBScore.ShouldBe(0.50m);
        ranking.WinnerIndex.ShouldBe(0);
    }

    [Fact]
    public void CalculateRanking_ShouldWeightSharpeRatio40Percent()
    {
        // Arrange
        var metricsA = CreateMetrics(sharpeRatio: 2.0m);
        var metricsB = CreateMetrics(sharpeRatio: 1.0m);

        // Act
        var ranking = ComparisonRanking.CalculateRanking(metricsA, metricsB);

        // Assert
        ranking.MetricBreakdown["Sharpe Ratio"].VariantAPoints.ShouldBe(0.40m);
        ranking.MetricBreakdown["Sharpe Ratio"].VariantBPoints.ShouldBe(0m);
    }

    [Fact]
    public void CalculateRanking_ShouldWeightAnnualizedReturn30Percent()
    {
        // Arrange
        var metricsA = CreateMetrics(sharpeRatio: 1.5m, annualizedReturn: 15m);
        var metricsB = CreateMetrics(sharpeRatio: 1.5m, annualizedReturn: 10m);

        // Act
        var ranking = ComparisonRanking.CalculateRanking(metricsA, metricsB);

        // Assert
        ranking.MetricBreakdown["Annualized Return"].VariantAPoints.ShouldBe(0.30m);
        ranking.MetricBreakdown["Annualized Return"].VariantBPoints.ShouldBe(0m);
    }

    [Fact]
    public void CalculateRanking_ShouldWeightMaxDrawdown20Percent()
    {
        // Arrange
        var metricsA = CreateMetrics(maxDrawdownPercentage: -10m);
        var metricsB = CreateMetrics(maxDrawdownPercentage: -20m);

        // Act
        var ranking = ComparisonRanking.CalculateRanking(metricsA, metricsB);

        // Assert - Lower drawdown is better, so A should get points
        ranking.MetricBreakdown["Max Drawdown %"].VariantAPoints.ShouldBe(0.20m);
        ranking.MetricBreakdown["Max Drawdown %"].VariantBPoints.ShouldBe(0m);
    }

    [Fact]
    public void CalculateRanking_ShouldWeightWinRate10Percent()
    {
        // Arrange
        var metricsA = CreateMetrics(winRate: 60m);
        var metricsB = CreateMetrics(winRate: 50m);

        // Act
        var ranking = ComparisonRanking.CalculateRanking(metricsA, metricsB);

        // Assert
        ranking.MetricBreakdown["Win Rate"].VariantAPoints.ShouldBe(0.10m);
        ranking.MetricBreakdown["Win Rate"].VariantBPoints.ShouldBe(0m);
    }

    [Fact]
    public void CalculateRanking_LowerDrawdownShouldScore_WhenOtherMetricsEqual()
    {
        // Arrange
        var metricsA = CreateMetrics(maxDrawdownPercentage: -10m);
        var metricsB = CreateMetrics(maxDrawdownPercentage: -20m);

        // Act
        var ranking = ComparisonRanking.CalculateRanking(metricsA, metricsB);

        // Assert
        ranking.MetricBreakdown["Max Drawdown %"].VariantAPoints.ShouldBe(0.20m);
        ranking.MetricBreakdown["Max Drawdown %"].VariantBPoints.ShouldBe(0m);
    }

    [Fact]
    public void CalculateRanking_ShouldIncludeAdditionalMetricsWithZeroWeight()
    {
        // Arrange
        var metricsA = CreateMetrics();
        var metricsB = CreateMetrics();

        // Act
        var ranking = ComparisonRanking.CalculateRanking(metricsA, metricsB);

        // Assert
        ranking.MetricBreakdown.ShouldContainKey("Total Return %");
        ranking.MetricBreakdown.ShouldContainKey("Profit Factor");
        ranking.MetricBreakdown["Total Return %"].VariantAPoints.ShouldBe(0m);
        ranking.MetricBreakdown["Total Return %"].VariantBPoints.ShouldBe(0m);
    }

    [Fact]
    public void MetricComparison_ShouldCalculateDifference()
    {
        // Arrange
        var comparison = new MetricComparison(10m, 8m, 0.5m, 0m, true);

        // Act & Assert
        comparison.Difference.ShouldBe(2m);
    }

    [Fact]
    public void MetricComparison_ShouldCalculatePercentageDifference()
    {
        // Arrange
        var comparison = new MetricComparison(10m, 8m, 0.5m, 0m, true);

        // Act & Assert
        comparison.PercentageDifference.ShouldBe(25m); // (10-8)/8 * 100 = 25%
    }

    [Fact]
    public void MetricComparison_WithZeroDivisor_ShouldReturnZeroPercentageDifference()
    {
        // Arrange
        var comparison = new MetricComparison(10m, 0m, 0.5m, 0m, true);

        // Act & Assert
        comparison.PercentageDifference.ShouldBe(0m);
    }

    private PerformanceMetrics CreateMetrics(
        decimal sharpeRatio = 1.5m,
        decimal annualizedReturn = 10m,
        decimal maxDrawdownPercentage = -15m,
        decimal winRate = 55m,
        decimal totalReturnPercentage = 20m,
        decimal profitFactor = 1.8m)
    {
        return new PerformanceMetrics(
            InitialCapital: 10000m,
            FinalEquity: 12000m,
            TotalReturn: 2000m,
            TotalReturnPercentage: totalReturnPercentage,
            AnnualizedReturn: annualizedReturn,
            TotalTrades: 50,
            WinningTrades: 28,
            LosingTrades: 22,
            WinRate: winRate,
            AverageWin: 150m,
            AverageLoss: -100m,
            LargestWin: 500m,
            LargestLoss: -300m,
            ProfitFactor: profitFactor,
            MaxConsecutiveWins: 5,
            MaxConsecutiveLosses: 3,
            MaxDrawdown: 1500m,
            MaxDrawdownPercentage: maxDrawdownPercentage,
            SharpeRatio: sharpeRatio,
            Volatility: 12m,
            TotalDays: 250,
            DaysInMarket: 100,
            MarketExposurePercentage: 40m);
    }
}
