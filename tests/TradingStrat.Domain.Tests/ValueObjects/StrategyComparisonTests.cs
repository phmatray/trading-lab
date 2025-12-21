using Shouldly;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.ValueObjects;

public class StrategyComparisonTests
{
    [Fact]
    public void WinningVariant_WhenVariantAWins_ShouldReturnVariantA()
    {
        // Arrange
        var variantA = CreateVariant("Variant A", "ma");
        var variantB = CreateVariant("Variant B", "ma");
        var resultA = CreateBacktestResult("Strategy A");
        var resultB = CreateBacktestResult("Strategy B");
        var ranking = CreateRanking(winnerIndex: 1);

        var comparison = new StrategyComparison(
            variantA, resultA, variantB, resultB, ranking, "TEST", DateTime.Now);

        // Act & Assert
        comparison.WinningVariant.ShouldBe(variantA);
        comparison.Winner.ShouldBe(1);
    }

    [Fact]
    public void WinningVariant_WhenVariantBWins_ShouldReturnVariantB()
    {
        // Arrange
        var variantA = CreateVariant("Variant A", "ma");
        var variantB = CreateVariant("Variant B", "ma");
        var resultA = CreateBacktestResult("Strategy A");
        var resultB = CreateBacktestResult("Strategy B");
        var ranking = CreateRanking(winnerIndex: 2);

        var comparison = new StrategyComparison(
            variantA, resultA, variantB, resultB, ranking, "TEST", DateTime.Now);

        // Act & Assert
        comparison.WinningVariant.ShouldBe(variantB);
        comparison.Winner.ShouldBe(2);
    }

    [Fact]
    public void WinningVariant_WhenTie_ShouldReturnNull()
    {
        // Arrange
        var variantA = CreateVariant("Variant A", "ma");
        var variantB = CreateVariant("Variant B", "ma");
        var resultA = CreateBacktestResult("Strategy A");
        var resultB = CreateBacktestResult("Strategy B");
        var ranking = CreateRanking(winnerIndex: 0);

        var comparison = new StrategyComparison(
            variantA, resultA, variantB, resultB, ranking, "TEST", DateTime.Now);

        // Act & Assert
        comparison.WinningVariant.ShouldBeNull();
        comparison.Winner.ShouldBe(0);
    }

    [Fact]
    public void WinningResult_WhenVariantAWins_ShouldReturnResultA()
    {
        // Arrange
        var variantA = CreateVariant("Variant A", "ma");
        var variantB = CreateVariant("Variant B", "ma");
        var resultA = CreateBacktestResult("Strategy A");
        var resultB = CreateBacktestResult("Strategy B");
        var ranking = CreateRanking(winnerIndex: 1);

        var comparison = new StrategyComparison(
            variantA, resultA, variantB, resultB, ranking, "TEST", DateTime.Now);

        // Act & Assert
        comparison.WinningResult.ShouldBe(resultA);
    }

    [Fact]
    public void WinningResult_WhenVariantBWins_ShouldReturnResultB()
    {
        // Arrange
        var variantA = CreateVariant("Variant A", "ma");
        var variantB = CreateVariant("Variant B", "ma");
        var resultA = CreateBacktestResult("Strategy A");
        var resultB = CreateBacktestResult("Strategy B");
        var ranking = CreateRanking(winnerIndex: 2);

        var comparison = new StrategyComparison(
            variantA, resultA, variantB, resultB, ranking, "TEST", DateTime.Now);

        // Act & Assert
        comparison.WinningResult.ShouldBe(resultB);
    }

    [Fact]
    public void WinningResult_WhenTie_ShouldReturnNull()
    {
        // Arrange
        var variantA = CreateVariant("Variant A", "ma");
        var variantB = CreateVariant("Variant B", "ma");
        var resultA = CreateBacktestResult("Strategy A");
        var resultB = CreateBacktestResult("Strategy B");
        var ranking = CreateRanking(winnerIndex: 0);

        var comparison = new StrategyComparison(
            variantA, resultA, variantB, resultB, ranking, "TEST", DateTime.Now);

        // Act & Assert
        comparison.WinningResult.ShouldBeNull();
    }

    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        StrategyVariant variantA = CreateVariant("Variant A", "ma");
        StrategyVariant variantB = CreateVariant("Variant B", "ma");
        BacktestResult resultA = CreateBacktestResult("Strategy A");
        BacktestResult resultB = CreateBacktestResult("Strategy B");
        ComparisonRanking ranking = CreateRanking(winnerIndex: 1);
        string ticker = "AAPL";
        DateTime comparisonDate = new DateTime(2024, 12, 7);

        // Act
        StrategyComparison comparison = new StrategyComparison(
            variantA, resultA, variantB, resultB, ranking, ticker, comparisonDate);

        // Assert
        comparison.VariantA.ShouldBe(variantA);
        comparison.ResultA.ShouldBe(resultA);
        comparison.VariantB.ShouldBe(variantB);
        comparison.ResultB.ShouldBe(resultB);
        comparison.Ranking.ShouldBe(ranking);
        comparison.Ticker.ShouldBe(ticker);
        comparison.ComparisonDate.ShouldBe(comparisonDate);
    }

    private StrategyVariant CreateVariant(string label, string type)
    {
        return new StrategyVariant(
            label,
            type,
            new Dictionary<string, object>(),
            "Test variant");
    }

    private BacktestResult CreateBacktestResult(string strategyName)
    {
        var metrics = new PerformanceMetrics(
            10000m, 12000m, 2000m, 20m, 10m,
            50, 30, 20, 60m,
            150m, -100m, 500m, -300m, 1.5m,
            5, 3, 1500m, -15m, 1.8m, 12m,
            250, 100, 40m);

        return new BacktestResult(
            strategyName,
            "Test strategy",
            new Dictionary<string, object>(),
            "TEST",
            DateTime.Today.AddYears(-1),
            DateTime.Today,
            10000m,
            0.001m,
            1.0m,
            new List<Trade>(),
            new List<EquityPoint>(),
            metrics);
    }

    private ComparisonRanking CreateRanking(int winnerIndex)
    {
        return new ComparisonRanking(
            0.75m,
            0.65m,
            new Dictionary<string, MetricComparison>(),
            winnerIndex);
    }
}
