using Shouldly;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.Services;

public class PortfolioPerformanceServiceTests
{
    private readonly PortfolioPerformanceService _service;

    public PortfolioPerformanceServiceTests()
    {
        _service = new PortfolioPerformanceService();
    }

    #region CalculateMetrics - Null Checks

    [Fact]
    public void CalculateMetrics_WithNullSnapshot_ShouldThrow()
    {
        // Arrange & Act
        Func<PortfolioMetrics> act = () => _service.CalculateMetrics(null!);

        // Assert
        ArgumentNullException ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("snapshot");
    }

    #endregion

    #region CalculateMetrics - Empty Portfolio

    [Fact]
    public void CalculateMetrics_WithEmptyPortfolio_ShouldReturnCashOnlyMetrics()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            PortfolioId: 1,
            PortfolioName: "Cash Only",
            SnapshotDate: DateTime.UtcNow,
            Cash: 10000m,
            Positions: new List<PositionSnapshot>(),
            TotalValue: 10000m,
            TotalCost: 10000m,
            UnrealizedGainLoss: 0m,
            UnrealizedGainLossPercentage: 0m
        );

        // Act
        var metrics = _service.CalculateMetrics(snapshot);

        // Assert
        metrics.ShouldNotBeNull();
        metrics.TotalValue.ShouldBe(10000m);
        metrics.TotalCost.ShouldBe(10000m);
        metrics.TotalReturn.ShouldBe(0m);
        metrics.TotalReturnPercentage.ShouldBe(0m);
        metrics.NumberOfPositions.ShouldBe(0);
        metrics.CashPercentage.ShouldBe(100m);
        metrics.LargestPositionPercentage.ShouldBe(0m);
        metrics.MostValuablePosition.ShouldBe("None");
    }

    #endregion

    #region CalculateMetrics - Single Position

    [Fact]
    public void CalculateMetrics_WithSinglePosition_ShouldCalculateCorrectly()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            PortfolioId: 1,
            PortfolioName: "Single Asset",
            SnapshotDate: DateTime.UtcNow,
            Cash: 5000m,
            Positions: new List<PositionSnapshot>
            {
                new PositionSnapshot("AAPL", 10, 100m, 150m, 1500m, 1000m, 500m, 50m, 23.08m)
            },
            TotalValue: 6500m,
            TotalCost: 6000m,
            UnrealizedGainLoss: 500m,
            UnrealizedGainLossPercentage: 8.33m
        );

        // Act
        var metrics = _service.CalculateMetrics(snapshot);

        // Assert
        metrics.TotalValue.ShouldBe(6500m);
        metrics.TotalCost.ShouldBe(6000m);
        metrics.TotalReturn.ShouldBe(500m);
        metrics.TotalReturnPercentage.ShouldBe(8.33m);
        metrics.NumberOfPositions.ShouldBe(1);
        metrics.CashPercentage.ShouldBeInRange(76.92m, 76.93m); // (5000 / 6500) * 100
        metrics.LargestPositionPercentage.ShouldBe(23.08m);
        metrics.MostValuablePosition.ShouldBe("AAPL");
    }

    #endregion

    #region CalculateMetrics - Multiple Positions

    [Fact]
    public void CalculateMetrics_WithMultiplePositions_ShouldIdentifyMostValuable()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            PortfolioId: 1,
            PortfolioName: "Diversified",
            SnapshotDate: DateTime.UtcNow,
            Cash: 10000m,
            Positions: new List<PositionSnapshot>
            {
                new PositionSnapshot("AAPL", 10, 100m, 150m, 1500m, 1000m, 500m, 50m, 6.67m),
                new PositionSnapshot("MSFT", 20, 200m, 250m, 5000m, 4000m, 1000m, 25m, 22.22m),
                new PositionSnapshot("GOOGL", 5, 1000m, 1200m, 6000m, 5000m, 1000m, 20m, 26.67m)
            },
            TotalValue: 22500m,
            TotalCost: 20000m,
            UnrealizedGainLoss: 2500m,
            UnrealizedGainLossPercentage: 12.5m
        );

        // Act
        var metrics = _service.CalculateMetrics(snapshot);

        // Assert
        metrics.NumberOfPositions.ShouldBe(3);
        metrics.MostValuablePosition.ShouldBe("GOOGL"); // $6,000 market value
        metrics.LargestPositionPercentage.ShouldBe(26.67m);
        metrics.CashPercentage.ShouldBeInRange(44.44m, 44.45m); // (10000 / 22500) * 100
    }

    #endregion

    #region CalculateMetrics - Diversification Ratio

    [Fact]
    public void CalculateMetrics_WithEqualAllocation_ShouldHaveHighDiversification()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            PortfolioId: 1,
            PortfolioName: "Balanced",
            SnapshotDate: DateTime.UtcNow,
            Cash: 0m,
            Positions: new List<PositionSnapshot>
            {
                new PositionSnapshot("AAPL", 10, 100m, 100m, 1000m, 1000m, 0m, 0m, 25m),
                new PositionSnapshot("MSFT", 10, 100m, 100m, 1000m, 1000m, 0m, 0m, 25m),
                new PositionSnapshot("GOOGL", 10, 100m, 100m, 1000m, 1000m, 0m, 0m, 25m),
                new PositionSnapshot("AMZN", 10, 100m, 100m, 1000m, 1000m, 0m, 0m, 25m)
            },
            TotalValue: 4000m,
            TotalCost: 4000m,
            UnrealizedGainLoss: 0m,
            UnrealizedGainLossPercentage: 0m
        );

        // Act
        var metrics = _service.CalculateMetrics(snapshot);

        // Assert
        // HHI = 4 * (0.25^2) = 4 * 0.0625 = 0.25
        // Diversification Ratio = 1 / 0.25 = 4
        metrics.DiversificationRatio.ShouldBe(4m);
    }

    [Fact]
    public void CalculateMetrics_WithConcentratedAllocation_ShouldHaveLowDiversification()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            PortfolioId: 1,
            PortfolioName: "Concentrated",
            SnapshotDate: DateTime.UtcNow,
            Cash: 0m,
            Positions: new List<PositionSnapshot>
            {
                new PositionSnapshot("AAPL", 100, 100m, 100m, 10000m, 10000m, 0m, 0m, 90m),
                new PositionSnapshot("MSFT", 10, 100m, 100m, 1000m, 1000m, 0m, 0m, 9m),
                new PositionSnapshot("GOOGL", 1, 100m, 100m, 111m, 111m, 0m, 0m, 1m)
            },
            TotalValue: 11111m,
            TotalCost: 11111m,
            UnrealizedGainLoss: 0m,
            UnrealizedGainLossPercentage: 0m
        );

        // Act
        var metrics = _service.CalculateMetrics(snapshot);

        // Assert
        // HHI = 0.9^2 + 0.09^2 + 0.01^2 = 0.81 + 0.0081 + 0.0001 = 0.8182
        // Diversification Ratio = 1 / 0.8182 ≈ 1.22
        metrics.DiversificationRatio.ShouldBeLessThan(2m);
        metrics.DiversificationRatio.ShouldBeGreaterThan(1m);
    }

    #endregion

    #region CalculateMetrics - Volatility and Sharpe (No Historical Data)

    [Fact]
    public void CalculateMetrics_WithoutHistoricalData_ShouldReturnZeroVolatility()
    {
        // Arrange
        var snapshot = CreateSimpleSnapshot();

        // Act
        var metrics = _service.CalculateMetrics(snapshot);

        // Assert
        metrics.PortfolioVolatility.ShouldBe(0m);
        metrics.PortfolioSharpeRatio.ShouldBe(0m);
        metrics.DailyReturn.ShouldBe(0m);
        metrics.DailyReturnPercentage.ShouldBe(0m);
    }

    [Fact]
    public void CalculateMetrics_WithInsufficientHistoricalData_ShouldReturnZeroVolatility()
    {
        // Arrange
        var snapshot = CreateSimpleSnapshot();
        var historicalPoints = new List<PortfolioPerformancePoint>
        {
            new PortfolioPerformancePoint(DateTime.Today.AddDays(-1), 10000m, 10000m, 0m, 0m)
        };

        // Act
        var metrics = _service.CalculateMetrics(snapshot, historicalPoints);

        // Assert
        metrics.PortfolioVolatility.ShouldBe(0m);
        metrics.PortfolioSharpeRatio.ShouldBe(0m);
    }

    #endregion

    #region CalculateMetrics - Volatility and Sharpe (With Historical Data)

    [Fact]
    public void CalculateMetrics_WithHistoricalData_ShouldCalculateVolatility()
    {
        // Arrange
        var snapshot = CreateSimpleSnapshot();
        var historicalPoints = new List<PortfolioPerformancePoint>
        {
            new PortfolioPerformancePoint(DateTime.Today.AddDays(-4), 10000m, 10000m, 0m, 0m),
            new PortfolioPerformancePoint(DateTime.Today.AddDays(-3), 10100m, 10000m, 100m, 100m),
            new PortfolioPerformancePoint(DateTime.Today.AddDays(-2), 10050m, 10000m, 50m, -50m),
            new PortfolioPerformancePoint(DateTime.Today.AddDays(-1), 10200m, 10000m, 200m, 150m),
            new PortfolioPerformancePoint(DateTime.Today, 10150m, 10000m, 150m, -50m)
        };

        // Act
        var metrics = _service.CalculateMetrics(snapshot, historicalPoints);

        // Assert
        metrics.PortfolioVolatility.ShouldBeGreaterThan(0m);
        metrics.PortfolioSharpeRatio.ShouldNotBe(0m);
    }

    [Fact]
    public void CalculateMetrics_WithHistoricalData_ShouldCaptureMostRecentDailyReturn()
    {
        // Arrange
        var snapshot = CreateSimpleSnapshot();
        var historicalPoints = new List<PortfolioPerformancePoint>
        {
            new PortfolioPerformancePoint(DateTime.Today.AddDays(-3), 10000m, 10000m, 0m, 0m),
            new PortfolioPerformancePoint(DateTime.Today.AddDays(-2), 10100m, 10000m, 100m, 100m),
            new PortfolioPerformancePoint(DateTime.Today.AddDays(-1), 10300m, 10000m, 300m, 200m)
        };

        // Act
        var metrics = _service.CalculateMetrics(snapshot, historicalPoints);

        // Assert
        metrics.DailyReturn.ShouldBe(200m); // Most recent daily return
    }

    [Fact]
    public void CalculateMetrics_WithZeroVolatility_ShouldReturnZeroSharpe()
    {
        // Arrange
        var snapshot = CreateSimpleSnapshot();
        var historicalPoints = new List<PortfolioPerformancePoint>
        {
            new PortfolioPerformancePoint(DateTime.Today.AddDays(-2), 10000m, 10000m, 0m, 0m),
            new PortfolioPerformancePoint(DateTime.Today.AddDays(-1), 10000m, 10000m, 0m, 0m),
            new PortfolioPerformancePoint(DateTime.Today, 10000m, 10000m, 0m, 0m)
        };

        // Act
        var metrics = _service.CalculateMetrics(snapshot, historicalPoints);

        // Assert
        metrics.PortfolioVolatility.ShouldBe(0m);
        metrics.PortfolioSharpeRatio.ShouldBe(0m);
    }

    #endregion

    #region CalculateMetrics - Daily Return Percentage

    [Fact]
    public void CalculateMetrics_WithPositiveDailyReturn_ShouldCalculatePercentage()
    {
        // Arrange
        var snapshot = CreateSimpleSnapshot();
        var historicalPoints = new List<PortfolioPerformancePoint>
        {
            new PortfolioPerformancePoint(DateTime.Today.AddDays(-2), 10000m, 10000m, 0m, 0m),
            new PortfolioPerformancePoint(DateTime.Today.AddDays(-1), 10100m, 10000m, 100m, 100m),
            new PortfolioPerformancePoint(DateTime.Today, 10300m, 10000m, 300m, 200m)
        };

        // Act
        var metrics = _service.CalculateMetrics(snapshot, historicalPoints);

        // Assert
        metrics.DailyReturn.ShouldBe(200m);
        // DailyReturnPercentage = (200 / (10300 - 200)) * 100 = (200 / 10100) * 100 ≈ 1.98%
        metrics.DailyReturnPercentage.ShouldBeInRange(1.9m, 2.0m);
    }

    #endregion

    #region CalculateMetrics - Correlation and Beta

    [Fact]
    public void CalculateMetrics_ShouldReturnNullForCorrelationAndBeta()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            PortfolioId: 1,
            PortfolioName: "Multi-Asset",
            SnapshotDate: DateTime.UtcNow,
            Cash: 5000m,
            Positions: new List<PositionSnapshot>
            {
                new PositionSnapshot("AAPL", 10, 100m, 150m, 1500m, 1000m, 500m, 50m, 20m),
                new PositionSnapshot("MSFT", 5, 200m, 250m, 1250m, 1000m, 250m, 25m, 16.67m)
            },
            TotalValue: 7750m,
            TotalCost: 7000m,
            UnrealizedGainLoss: 750m,
            UnrealizedGainLossPercentage: 10.71m
        );

        // Act
        var metrics = _service.CalculateMetrics(snapshot);

        // Assert
        // AverageCorrelation and PositionBetas are null (require historical position-level data)
        // See PortfolioPerformanceService.cs for implementation prerequisites and benefits
        metrics.AverageCorrelation.ShouldBeNull();
        metrics.PositionBetas.ShouldBeNull();
    }

    #endregion

    #region CalculateMetrics - Zero Total Value Edge Case

    [Fact]
    public void CalculateMetrics_WithZeroTotalValue_ShouldSetCashPercentageToZero()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            PortfolioId: 1,
            PortfolioName: "Empty Portfolio",
            SnapshotDate: DateTime.UtcNow,
            Cash: 0m,
            Positions: new List<PositionSnapshot>(),
            TotalValue: 0m,
            TotalCost: 0m,
            UnrealizedGainLoss: 0m,
            UnrealizedGainLossPercentage: 0m
        );

        // Act
        var metrics = _service.CalculateMetrics(snapshot);

        // Assert
        metrics.CashPercentage.ShouldBe(0m);
    }

    #endregion

    #region CalculateMetrics - Positive and Negative Returns

    [Fact]
    public void CalculateMetrics_WithPositiveReturns_ShouldCalculateCorrectly()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            PortfolioId: 1,
            PortfolioName: "Winning Portfolio",
            SnapshotDate: DateTime.UtcNow,
            Cash: 5000m,
            Positions: new List<PositionSnapshot>
            {
                new PositionSnapshot("AAPL", 10, 100m, 200m, 2000m, 1000m, 1000m, 100m, 28.57m)
            },
            TotalValue: 7000m,
            TotalCost: 6000m,
            UnrealizedGainLoss: 1000m,
            UnrealizedGainLossPercentage: 16.67m
        );

        // Act
        var metrics = _service.CalculateMetrics(snapshot);

        // Assert
        metrics.TotalReturn.ShouldBe(1000m);
        metrics.TotalReturnPercentage.ShouldBe(16.67m);
    }

    [Fact]
    public void CalculateMetrics_WithNegativeReturns_ShouldCalculateCorrectly()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            PortfolioId: 1,
            PortfolioName: "Losing Portfolio",
            SnapshotDate: DateTime.UtcNow,
            Cash: 5000m,
            Positions: new List<PositionSnapshot>
            {
                new PositionSnapshot("AAPL", 10, 200m, 100m, 1000m, 2000m, -1000m, -50m, 16.67m)
            },
            TotalValue: 6000m,
            TotalCost: 7000m,
            UnrealizedGainLoss: -1000m,
            UnrealizedGainLossPercentage: -14.29m
        );

        // Act
        var metrics = _service.CalculateMetrics(snapshot);

        // Assert
        metrics.TotalReturn.ShouldBe(-1000m);
        metrics.TotalReturnPercentage.ShouldBe(-14.29m);
    }

    #endregion

    #region CalculateMetrics - Annualized Metrics

    [Fact]
    public void CalculateMetrics_WithVolatileReturns_ShouldAnnualizeCorrectly()
    {
        // Arrange
        var snapshot = CreateSimpleSnapshot();
        var historicalPoints = new List<PortfolioPerformancePoint>();

        // Create 30 days of volatile returns
        decimal value = 10000m;
        for (int i = 30; i >= 0; i--)
        {
            decimal dailyReturn = i % 2 == 0 ? 50m : -50m; // Alternating +50/-50
            value += dailyReturn;
            historicalPoints.Add(new PortfolioPerformancePoint(
                DateTime.Today.AddDays(-i),
                value,
                10000m,
                value - 10000m,
                dailyReturn
            ));
        }

        // Act
        var metrics = _service.CalculateMetrics(snapshot, historicalPoints);

        // Assert
        // Volatility is annualized by multiplying by sqrt(252)
        metrics.PortfolioVolatility.ShouldBeGreaterThan(0m);
        // Sharpe is also annualized
        metrics.PortfolioSharpeRatio.ShouldNotBe(0m);
    }

    #endregion

    #region Helper Methods

    private static PortfolioSnapshot CreateSimpleSnapshot()
    {
        return new PortfolioSnapshot(
            PortfolioId: 1,
            PortfolioName: "Test Portfolio",
            SnapshotDate: DateTime.UtcNow,
            Cash: 10000m,
            Positions: new List<PositionSnapshot>(),
            TotalValue: 10000m,
            TotalCost: 10000m,
            UnrealizedGainLoss: 0m,
            UnrealizedGainLossPercentage: 0m
        );
    }

    #endregion
}
