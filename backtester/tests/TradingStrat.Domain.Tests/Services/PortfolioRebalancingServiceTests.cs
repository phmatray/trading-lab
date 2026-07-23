using Shouldly;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.Services;

public class PortfolioRebalancingServiceTests
{
    private readonly PortfolioRebalancingService _service;

    public PortfolioRebalancingServiceTests()
    {
        _service = new PortfolioRebalancingService();
    }

    #region CalculateRebalancing - Invalid Target Weights

    [Fact]
    public void CalculateRebalancing_WithInvalidTargetWeights_ShouldThrow()
    {
        // Arrange
        PortfolioSnapshot snapshot = CreateSimpleSnapshot();

        var targetWeights = new AllocationWeights(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 60m,
                ["MSFT"] = 30m
            },
            cashPercentage: 0m); // Total = 90%, not 100%

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m,
            ["MSFT"] = 250m
        };

        // Act
        Func<RebalancingPlan> act = () => _service.CalculateRebalancing(snapshot, targetWeights, currentPrices, 0.001m, 1m);

        // Assert
        ArgumentException ex = Should.Throw<ArgumentException>(act);
        ex.Message.ShouldContain("Target allocations must sum to 100%");
        ex.ParamName.ShouldBe("targetWeights");
    }

    #endregion

    #region CalculateRebalancing - Missing Price Data

    [Fact]
    public void CalculateRebalancing_WithMissingPriceForTargetTicker_ShouldThrow()
    {
        // Arrange
        PortfolioSnapshot snapshot = CreateSimpleSnapshot();

        var targetWeights = new AllocationWeights(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 50m,
                ["GOOGL"] = 50m
            },
            cashPercentage: 0m);

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m
            // Missing GOOGL
        };

        // Act
        Func<RebalancingPlan> act = () => _service.CalculateRebalancing(snapshot, targetWeights, currentPrices, 0.001m, 1m);

        // Assert
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain("No current price available for GOOGL");
    }

    [Fact]
    public void CalculateRebalancing_WithMissingPriceForExistingPosition_ShouldThrow()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            portfolioId: 1,
            portfolioName: "Test Portfolio",
            snapshotDate: DateTime.UtcNow,
            cash: 10000m,
            positions: new List<PositionSnapshot>
            {
                new PositionSnapshot("AAPL", 10, 100m, 150m, 1500m, 1000m, 500m, 50m, 20m)
            },
            totalValue: 11500m,
            totalCost: 11000m,
            unrealizedGainLoss: 500m,
            unrealizedGainLossPercentage: 4.54m
        );

        var targetWeights = new AllocationWeights(
            new Dictionary<string, decimal>(),
            cashPercentage: 100m); // Liquidate all positions

        var currentPrices = new Dictionary<string, decimal>(); // Missing AAPL price

        // Act
        Func<RebalancingPlan> act = () => _service.CalculateRebalancing(snapshot, targetWeights, currentPrices, 0.001m, 1m);

        // Assert
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain("No current price available for AAPL");
    }

    #endregion

    #region CalculateRebalancing - Buy Signals

    [Fact]
    public void CalculateRebalancing_WithNewPosition_ShouldGenerateBuySignal()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            portfolioId: 1,
            portfolioName: "Test Portfolio",
            snapshotDate: DateTime.UtcNow,
            cash: 10000m,
            positions: new List<PositionSnapshot>(),
            totalValue: 10000m,
            totalCost: 10000m,
            unrealizedGainLoss: 0m,
            unrealizedGainLossPercentage: 0m
        );

        var targetWeights = new AllocationWeights(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 50m
            },
            cashPercentage: 50m);

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m
        };

        // Act
        RebalancingPlan plan = _service.CalculateRebalancing(snapshot, targetWeights, currentPrices, 0.001m, 1m);

        // Assert
        plan.ShouldNotBeNull();
        plan.Signals.Count.ShouldBe(1);

        RebalancingSignal signal = plan.Signals[0];
        signal.Ticker.ShouldBe("AAPL");
        signal.Action.ShouldBe(RebalancingAction.Buy);
        signal.CurrentQuantity.ShouldBe(0);
        signal.TargetQuantity.ShouldBe(33); // (10000 * 0.50) / 150 = 33.33 -> 33
        signal.QuantityDelta.ShouldBe(33);
        signal.CurrentAllocation.ShouldBe(0m);
        signal.TargetAllocation.ShouldBe(50m);
    }

    [Fact]
    public void CalculateRebalancing_WithInsufficientCash_ShouldMarkNotExecutable()
    {
        // Arrange - portfolio has positions with some cash
        var snapshot = new PortfolioSnapshot(
            portfolioId: 1,
            portfolioName: "Low Cash Portfolio",
            snapshotDate: DateTime.UtcNow,
            cash: 100m, // Only $100 cash
            positions: new List<PositionSnapshot>
            {
                new PositionSnapshot("MSFT", 2, 250m, 250m, 500m, 500m, 0m, 0m, 83.33m)
            },
            totalValue: 600m,
            totalCost: 600m,
            unrealizedGainLoss: 0m,
            unrealizedGainLossPercentage: 0m
        );

        var targetWeights = new AllocationWeights(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 90m,    // Need to buy $540 worth of AAPL
                ["MSFT"] = 10m     // Need to sell some MSFT
            },
            cashPercentage: 0m);

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m,  // Need to buy 3 shares = $450 + commission
            ["MSFT"] = 250m
        };

        // Act
        RebalancingPlan plan = _service.CalculateRebalancing(snapshot, targetWeights, currentPrices, 0.01m, 5m);

        // Assert
        // Needs to buy AAPL but doesn't have enough cash (even after selling 1 share of MSFT)
        plan.IsExecutable.ShouldBeFalse();
        plan.AvailableCash.ShouldBe(100m);
        plan.RequiredCash.ShouldBeGreaterThan(100m);
    }

    #endregion

    #region CalculateRebalancing - Sell Signals

    [Fact]
    public void CalculateRebalancing_WithPositionToLiquidate_ShouldGenerateSellSignal()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            portfolioId: 1,
            portfolioName: "Test Portfolio",
            snapshotDate: DateTime.UtcNow,
            cash: 5000m,
            positions: new List<PositionSnapshot>
            {
                new PositionSnapshot("AAPL", 10, 100m, 150m, 1500m, 1000m, 500m, 50m, 23.08m)
            },
            totalValue: 6500m,
            totalCost: 6000m,
            unrealizedGainLoss: 500m,
            unrealizedGainLossPercentage: 8.33m
        );

        var targetWeights = new AllocationWeights(
            new Dictionary<string, decimal>(),
            cashPercentage: 100m); // Liquidate everything

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m
        };

        // Act
        RebalancingPlan plan = _service.CalculateRebalancing(snapshot, targetWeights, currentPrices, 0.001m, 1m);

        // Assert
        plan.Signals.Count.ShouldBe(1);

        RebalancingSignal signal = plan.Signals[0];
        signal.Ticker.ShouldBe("AAPL");
        signal.Action.ShouldBe(RebalancingAction.Sell);
        signal.CurrentQuantity.ShouldBe(10);
        signal.TargetQuantity.ShouldBe(0);
        signal.QuantityDelta.ShouldBe(-10);
        signal.EstimatedCost.ShouldBeLessThan(0); // Negative for sell
    }

    [Fact]
    public void CalculateRebalancing_WithPartialSell_ShouldGenerateSellSignal()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            portfolioId: 1,
            portfolioName: "Test Portfolio",
            snapshotDate: DateTime.UtcNow,
            cash: 5000m,
            positions: new List<PositionSnapshot>
            {
                new PositionSnapshot("AAPL", 20, 100m, 150m, 3000m, 2000m, 1000m, 50m, 37.5m)
            },
            totalValue: 8000m,
            totalCost: 7000m,
            unrealizedGainLoss: 1000m,
            unrealizedGainLossPercentage: 14.29m
        );

        var targetWeights = new AllocationWeights(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 18.75m // Half of current allocation
            },
            cashPercentage: 81.25m);

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m
        };

        // Act
        RebalancingPlan plan = _service.CalculateRebalancing(snapshot, targetWeights, currentPrices, 0.001m, 1m);

        // Assert
        RebalancingSignal signal = plan.Signals[0];
        signal.Action.ShouldBe(RebalancingAction.Sell);
        signal.CurrentQuantity.ShouldBe(20);
        signal.TargetQuantity.ShouldBe(10); // (8000 * 0.1875) / 150 = 10
        signal.QuantityDelta.ShouldBe(-10);
    }

    #endregion

    #region CalculateRebalancing - Hold Signals

    [Fact]
    public void CalculateRebalancing_WithNoChange_ShouldGenerateHoldSignal()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            portfolioId: 1,
            portfolioName: "Balanced Portfolio",
            snapshotDate: DateTime.UtcNow,
            cash: 5000m,
            positions: new List<PositionSnapshot>
            {
                new PositionSnapshot("AAPL", 33, 150m, 150m, 4950m, 4950m, 0m, 0m, 49.75m)
            },
            totalValue: 9950m,
            totalCost: 9950m,
            unrealizedGainLoss: 0m,
            unrealizedGainLossPercentage: 0m
        );

        var targetWeights = new AllocationWeights(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 49.75m // Matches current
            },
            cashPercentage: 50.25m);

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m
        };

        // Act
        RebalancingPlan plan = _service.CalculateRebalancing(snapshot, targetWeights, currentPrices, 0.001m, 1m);

        // Assert
        RebalancingSignal signal = plan.Signals[0];
        signal.Action.ShouldBe(RebalancingAction.Hold);
        signal.QuantityDelta.ShouldBe(0);
    }

    #endregion

    #region CalculateRebalancing - Commission Calculations

    [Fact]
    public void CalculateRebalancing_ShouldApplyCommissionPercentage()
    {
        // Arrange
        PortfolioSnapshot snapshot = CreateSimpleSnapshot();

        var targetWeights = new AllocationWeights(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 50m
            },
            cashPercentage: 50m);

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 100m
        };

        // Act
        RebalancingPlan plan = _service.CalculateRebalancing(snapshot, targetWeights, currentPrices, 0.01m, 0m);

        // Assert
        RebalancingSignal signal = plan.Signals[0];
        decimal grossCost = signal.QuantityDelta * 100m;
        decimal commission = grossCost * 0.01m; // 1% commission
        signal.EstimatedCost.ShouldBe(grossCost + commission);
    }

    [Fact]
    public void CalculateRebalancing_ShouldApplyMinimumCommission()
    {
        // Arrange
        PortfolioSnapshot snapshot = CreateSimpleSnapshot();

        var targetWeights = new AllocationWeights(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 1m // Small position
            },
            cashPercentage: 99m);

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 100m
        };

        // Act
        RebalancingPlan plan = _service.CalculateRebalancing(snapshot, targetWeights, currentPrices, 0.001m, 10m);

        // Assert
        RebalancingSignal signal = plan.Signals[0];
        // Even though percentage commission would be tiny, minimum commission applies
        signal.EstimatedCost.ShouldBeGreaterThan(10m);
    }

    #endregion

    #region CalculateRebalancing - Multiple Positions

    [Fact]
    public void CalculateRebalancing_WithMultiplePositions_ShouldGenerateMultipleSignals()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            portfolioId: 1,
            portfolioName: "Multi-Asset Portfolio",
            snapshotDate: DateTime.UtcNow,
            cash: 10000m,
            positions: new List<PositionSnapshot>
            {
                new PositionSnapshot("AAPL", 10, 100m, 150m, 1500m, 1000m, 500m, 50m, 12m),
                new PositionSnapshot("MSFT", 5, 200m, 250m, 1250m, 1000m, 250m, 25m, 10m)
            },
            totalValue: 12750m,
            totalCost: 12000m,
            unrealizedGainLoss: 750m,
            unrealizedGainLossPercentage: 6.25m
        );

        var targetWeights = new AllocationWeights(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 40m,
                ["MSFT"] = 30m,
                ["GOOGL"] = 30m
            },
            cashPercentage: 0m);

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m,
            ["MSFT"] = 250m,
            ["GOOGL"] = 1200m
        };

        // Act
        RebalancingPlan plan = _service.CalculateRebalancing(snapshot, targetWeights, currentPrices, 0.001m, 1m);

        // Assert
        plan.Signals.Count.ShouldBe(3);
        plan.Signals.ShouldContain(s => s.Ticker == "AAPL");
        plan.Signals.ShouldContain(s => s.Ticker == "MSFT");
        plan.Signals.ShouldContain(s => s.Ticker == "GOOGL");
    }

    #endregion

    #region CalculateRebalancing - Signal Ordering

    [Fact]
    public void CalculateRebalancing_ShouldOrderSignalsByAbsoluteCost()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            portfolioId: 1,
            portfolioName: "Test Portfolio",
            snapshotDate: DateTime.UtcNow,
            cash: 100000m,
            positions: new List<PositionSnapshot>(),
            totalValue: 100000m,
            totalCost: 100000m,
            unrealizedGainLoss: 0m,
            unrealizedGainLossPercentage: 0m
        );

        var targetWeights = new AllocationWeights(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 10m,  // $10,000 position
                ["GOOGL"] = 60m, // $60,000 position
                ["MSFT"] = 30m   // $30,000 position
            },
            cashPercentage: 0m);

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m,
            ["MSFT"] = 250m,
            ["GOOGL"] = 1200m
        };

        // Act
        RebalancingPlan plan = _service.CalculateRebalancing(snapshot, targetWeights, currentPrices, 0.001m, 1m);

        // Assert
        // Should be ordered by absolute cost: GOOGL (largest), MSFT, AAPL (smallest)
        plan.Signals[0].Ticker.ShouldBe("GOOGL");
        plan.Signals[1].Ticker.ShouldBe("MSFT");
        plan.Signals[2].Ticker.ShouldBe("AAPL");
    }

    #endregion

    #region CalculateRebalancing - Plan Metadata

    [Fact]
    public void CalculateRebalancing_ShouldCapturePlanMetadata()
    {
        // Arrange
        PortfolioSnapshot snapshot = CreateSimpleSnapshot();

        var targetWeights = new AllocationWeights(
            new Dictionary<string, decimal>(),
            cashPercentage: 100m);

        var currentPrices = new Dictionary<string, decimal>();

        // Act
        RebalancingPlan plan = _service.CalculateRebalancing(snapshot, targetWeights, currentPrices, 0.001m, 1m);

        // Assert
        plan.PortfolioId.ShouldBe(1);
        plan.CalculationDate.ShouldBeInRange(
            DateTime.UtcNow.AddSeconds(-5),
            DateTime.UtcNow.AddSeconds(1));
        plan.AvailableCash.ShouldBe(10000m);
    }

    #endregion

    #region CalculateRebalancing - Executable Plan

    [Fact]
    public void CalculateRebalancing_WithSufficientCash_ShouldMarkExecutable()
    {
        // Arrange
        var snapshot = new PortfolioSnapshot(
            portfolioId: 1,
            portfolioName: "Well-Funded Portfolio",
            snapshotDate: DateTime.UtcNow,
            cash: 50000m,
            positions: new List<PositionSnapshot>(),
            totalValue: 50000m,
            totalCost: 50000m,
            unrealizedGainLoss: 0m,
            unrealizedGainLossPercentage: 0m
        );

        var targetWeights = new AllocationWeights(
            new Dictionary<string, decimal>
            {
                ["AAPL"] = 50m
            },
            cashPercentage: 50m);

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m
        };

        // Act
        RebalancingPlan plan = _service.CalculateRebalancing(snapshot, targetWeights, currentPrices, 0.001m, 1m);

        // Assert
        plan.IsExecutable.ShouldBeTrue();
        plan.RequiredCash.ShouldBeLessThanOrEqualTo(plan.AvailableCash);
    }

    #endregion

    #region Helper Methods

    private static PortfolioSnapshot CreateSimpleSnapshot()
    {
        return new PortfolioSnapshot(
            portfolioId: 1,
            portfolioName: "Test Portfolio",
            snapshotDate: DateTime.UtcNow,
            cash: 10000m,
            positions: new List<PositionSnapshot>(),
            totalValue: 10000m,
            totalCost: 10000m,
            unrealizedGainLoss: 0m,
            unrealizedGainLossPercentage: 0m
        );
    }

    #endregion
}
