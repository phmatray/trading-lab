using Shouldly;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Tests.Services;

public class PortfolioValuationServiceTests
{
    private readonly PortfolioValuationService _service;

    public PortfolioValuationServiceTests()
    {
        _service = new PortfolioValuationService();
    }

    #region CalculateSnapshot - Null Checks

    [Fact]
    public void CalculateSnapshot_WithNullPortfolio_ShouldReturnFailure()
    {
        // Arrange
        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m
        };

        // Act
        Result<PortfolioSnapshot> result = _service.CalculateSnapshot(null!, currentPrices);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Type.ShouldBe(ErrorType.Validation);
        result.Errors[0].Code.ShouldBe("PORTFOLIO_REQUIRED");
    }

    [Fact]
    public void CalculateSnapshot_WithNullCurrentPrices_ShouldReturnFailure()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Id = 1,
            Name = "Test Portfolio",
            Cash = 10000m
        };

        // Act
        Result<PortfolioSnapshot> result = _service.CalculateSnapshot(portfolio, null!);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Type.ShouldBe(ErrorType.Validation);
        result.Errors[0].Code.ShouldBe("CURRENT_PRICES_REQUIRED");
    }

    #endregion

    #region CalculateSnapshot - Missing Price Data

    [Fact]
    public void CalculateSnapshot_WithMissingPriceForPosition_ShouldReturnFailure()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Id = 1,
            Name = "Test Portfolio",
            Cash = 10000m
        };

        portfolio.AddPosition(new Position
        {
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 100m
        });

        var currentPrices = new Dictionary<string, decimal>(); // Empty - no price for AAPL

        // Act
        Result<PortfolioSnapshot> result = _service.CalculateSnapshot(portfolio, currentPrices);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Type.ShouldBe(ErrorType.InsufficientData);
        result.Errors[0].Message.ShouldContain("No current price available for AAPL");
    }

    #endregion

    #region CalculateSnapshot - Empty Portfolio

    [Fact]
    public void CalculateSnapshot_WithEmptyPortfolio_ShouldReturnCashOnlySnapshot()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Id = 1,
            Name = "Cash Only Portfolio",
            Cash = 10000m
        };

        var currentPrices = new Dictionary<string, decimal>();

        // Act
        Result<PortfolioSnapshot> result = _service.CalculateSnapshot(portfolio, currentPrices);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        PortfolioSnapshot snapshot = result.Value;
        snapshot.ShouldNotBeNull();
        snapshot.PortfolioId.ShouldBe(1);
        snapshot.PortfolioName.ShouldBe("Cash Only Portfolio");
        snapshot.Cash.ShouldBe(10000m);
        snapshot.Positions.ShouldBeEmpty();
        snapshot.TotalValue.ShouldBe(10000m);
        snapshot.TotalCost.ShouldBe(10000m);
        snapshot.UnrealizedGainLoss.ShouldBe(0m);
        snapshot.UnrealizedGainLossPercentage.ShouldBe(0m);
    }

    #endregion

    #region CalculateSnapshot - Single Position

    [Fact]
    public void CalculateSnapshot_WithSinglePosition_ShouldCalculateCorrectly()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Id = 1,
            Name = "Test Portfolio",
            Cash = 5000m
        };

        portfolio.AddPosition(new Position
        {
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 100m
        });

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m
        };

        // Act
        Result<PortfolioSnapshot> result = _service.CalculateSnapshot(portfolio, currentPrices);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        PortfolioSnapshot snapshot = result.Value;
        snapshot.ShouldNotBeNull();
        snapshot.Cash.ShouldBe(5000m);
        snapshot.Positions.Count.ShouldBe(1);

        PositionSnapshot position = snapshot.Positions[0];
        position.Ticker.ShouldBe("AAPL");
        position.Quantity.ShouldBe(10);
        position.EntryPrice.ShouldBe(100m);
        position.CurrentPrice.ShouldBe(150m);
        position.MarketValue.ShouldBe(1500m); // 10 * 150
        position.CostBasis.ShouldBe(1000m); // 10 * 100
        position.UnrealizedGainLoss.ShouldBe(500m); // 1500 - 1000
        position.UnrealizedGainLossPercentage.ShouldBe(50m); // (500 / 1000) * 100

        snapshot.TotalValue.ShouldBe(6500m); // 5000 cash + 1500 market value
        snapshot.TotalCost.ShouldBe(6000m); // 5000 cash + 1000 cost basis
        snapshot.UnrealizedGainLoss.ShouldBe(500m);
        snapshot.UnrealizedGainLossPercentage.ShouldBeInRange(8.33m, 8.34m); // (500 / 6000) * 100
    }

    #endregion

    #region CalculateSnapshot - Multiple Positions

    [Fact]
    public void CalculateSnapshot_WithMultiplePositions_ShouldCalculateCorrectly()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Id = 1,
            Name = "Diversified Portfolio",
            Cash = 10000m
        };

        portfolio.AddPosition(new Position
        {
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 100m
        });

        portfolio.AddPosition(new Position
        {
            Ticker = "MSFT",
            Quantity = 20,
            EntryPrice = 200m
        });

        portfolio.AddPosition(new Position
        {
            Ticker = "GOOGL",
            Quantity = 5,
            EntryPrice = 1000m
        });

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m,
            ["MSFT"] = 250m,
            ["GOOGL"] = 1200m
        };

        // Act
        Result<PortfolioSnapshot> result = _service.CalculateSnapshot(portfolio, currentPrices);
        result.IsSuccess.ShouldBeTrue();
        PortfolioSnapshot snapshot = result.Value;

        // Assert
        snapshot.ShouldNotBeNull();
        snapshot.Positions.Count.ShouldBe(3);

        // Total market value = 10 * 150 + 20 * 250 + 5 * 1200 = 1500 + 5000 + 6000 = 12500
        // Total value = 10000 cash + 12500 = 22500
        snapshot.TotalValue.ShouldBe(22500m);

        // Total cost basis = 10 * 100 + 20 * 200 + 5 * 1000 = 1000 + 4000 + 5000 = 10000
        // Total cost = 10000 cash + 10000 = 20000
        snapshot.TotalCost.ShouldBe(20000m);

        snapshot.UnrealizedGainLoss.ShouldBe(2500m); // 22500 - 20000
        snapshot.UnrealizedGainLossPercentage.ShouldBe(12.5m); // (2500 / 20000) * 100
    }

    #endregion

    #region CalculateSnapshot - Allocation Percentages

    [Fact]
    public void CalculateSnapshot_ShouldCalculateAllocationPercentagesCorrectly()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Id = 1,
            Name = "Test Portfolio",
            Cash = 5000m
        };

        portfolio.AddPosition(new Position
        {
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 100m
        });

        portfolio.AddPosition(new Position
        {
            Ticker = "MSFT",
            Quantity = 10,
            EntryPrice = 100m
        });

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m, // Market value = 1500
            ["MSFT"] = 100m  // Market value = 1000
        };

        // Total value = 5000 + 1500 + 1000 = 7500
        // AAPL allocation = (1500 / 7500) * 100 = 20%
        // MSFT allocation = (1000 / 7500) * 100 = 13.333...%

        // Act
        Result<PortfolioSnapshot> result = _service.CalculateSnapshot(portfolio, currentPrices);
        result.IsSuccess.ShouldBeTrue();
        PortfolioSnapshot snapshot = result.Value;

        // Assert
        PositionSnapshot applePosition = snapshot.Positions.First(p => p.Ticker == "AAPL");
        applePosition.AllocationPercentage.ShouldBe(20m);

        PositionSnapshot msftPosition = snapshot.Positions.First(p => p.Ticker == "MSFT");
        msftPosition.AllocationPercentage.ShouldBeInRange(13.33m, 13.34m);
    }

    #endregion

    #region CalculateSnapshot - Gain/Loss Scenarios

    [Fact]
    public void CalculateSnapshot_WithPositionLoss_ShouldCalculateNegativeGainLoss()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Id = 1,
            Name = "Loss Portfolio",
            Cash = 5000m
        };

        portfolio.AddPosition(new Position
        {
            Ticker = "AAPL",
            Quantity = 10,
            EntryPrice = 200m
        });

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m
        };

        // Act
        Result<PortfolioSnapshot> result = _service.CalculateSnapshot(portfolio, currentPrices);
        result.IsSuccess.ShouldBeTrue();
        PortfolioSnapshot snapshot = result.Value;

        // Assert
        PositionSnapshot position = snapshot.Positions[0];
        position.UnrealizedGainLoss.ShouldBe(-500m); // (10 * 150) - (10 * 200) = 1500 - 2000 = -500
        position.UnrealizedGainLossPercentage.ShouldBe(-25m); // (-500 / 2000) * 100
    }

    [Fact]
    public void CalculateSnapshot_WithLowCostBasis_ShouldHandleGracefully()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Id = 1,
            Name = "Test Portfolio",
            Cash = 5000m
        };

        portfolio.AddPosition(new Position
        {
            Ticker = "FREE",
            Quantity = 10,
            EntryPrice = 0.01m // Very low entry price (near-free stock)
        });

        var currentPrices = new Dictionary<string, decimal>
        {
            ["FREE"] = 100m
        };

        // Act
        Result<PortfolioSnapshot> result = _service.CalculateSnapshot(portfolio, currentPrices);
        result.IsSuccess.ShouldBeTrue();
        PortfolioSnapshot snapshot = result.Value;

        // Assert
        PositionSnapshot position = snapshot.Positions[0];
        position.CostBasis.ShouldBe(0.10m); // 10 * 0.01
        position.UnrealizedGainLoss.ShouldBe(999.90m); // 1000 - 0.10
        position.UnrealizedGainLossPercentage.ShouldBe(999900m); // (999.90 / 0.10) * 100
    }

    #endregion

    #region CalculateTotalValue

    [Fact]
    public void CalculateTotalValue_WithCashOnly_ShouldReturnCash()
    {
        // Arrange
        decimal cash = 10000m;
        List<Position> positions = [];
        Dictionary<string, decimal> currentPrices = [];

        // Act
        decimal totalValue = _service.CalculateTotalValue(cash, positions, currentPrices);

        // Assert
        totalValue.ShouldBe(10000m);
    }

    [Fact]
    public void CalculateTotalValue_WithPositions_ShouldIncludeMarketValue()
    {
        // Arrange
        decimal cash = 5000m;
        List<Position> positions = new List<Position>
        {
            new Position
            {
                Ticker = "AAPL",
                Quantity = 10,
                EntryPrice = 100m
            },
            new Position
            {
                Ticker = "MSFT",
                Quantity = 5,
                EntryPrice = 200m
            }
        };

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m,
            ["MSFT"] = 250m
        };

        // Act
        decimal totalValue = _service.CalculateTotalValue(cash, positions, currentPrices);

        // Assert
        // 5000 + (10 * 150) + (5 * 250) = 5000 + 1500 + 1250 = 7750
        totalValue.ShouldBe(7750m);
    }

    [Fact]
    public void CalculateTotalValue_WithMissingPrice_ShouldSkipPosition()
    {
        // Arrange
        decimal cash = 5000m;
        List<Position> positions = new List<Position>
        {
            new Position
            {
                Ticker = "AAPL",
                Quantity = 10,
                EntryPrice = 100m
            },
            new Position
            {
                Ticker = "UNKNOWN",
                Quantity = 5,
                EntryPrice = 200m
            }
        };

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m
            // Missing "UNKNOWN"
        };

        // Act
        decimal totalValue = _service.CalculateTotalValue(cash, positions, currentPrices);

        // Assert
        // 5000 + (10 * 150) = 6500, UNKNOWN is skipped
        totalValue.ShouldBe(6500m);
    }

    [Fact]
    public void CalculateTotalValue_WithZeroCash_ShouldReturnPositionsValue()
    {
        // Arrange
        decimal cash = 0m;
        List<Position> positions = new List<Position>
        {
            new Position
            {
                Ticker = "AAPL",
                Quantity = 10,
                EntryPrice = 100m
            }
        };

        var currentPrices = new Dictionary<string, decimal>
        {
            ["AAPL"] = 150m
        };

        // Act
        decimal totalValue = _service.CalculateTotalValue(cash, positions, currentPrices);

        // Assert
        totalValue.ShouldBe(1500m);
    }

    #endregion

    #region CalculateSnapshot - Portfolio Metadata

    [Fact]
    public void CalculateSnapshot_ShouldCapturePortfolioMetadata()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Id = 42,
            Name = "My Growth Portfolio",
            Cash = 10000m
        };

        var currentPrices = new Dictionary<string, decimal>();

        // Act
        Result<PortfolioSnapshot> result = _service.CalculateSnapshot(portfolio, currentPrices);
        result.IsSuccess.ShouldBeTrue();
        PortfolioSnapshot snapshot = result.Value;

        // Assert
        snapshot.PortfolioId.ShouldBe(42);
        snapshot.PortfolioName.ShouldBe("My Growth Portfolio");
        snapshot.SnapshotDate.ShouldBeInRange(
            DateTime.UtcNow.AddSeconds(-5),
            DateTime.UtcNow.AddSeconds(1));
    }

    #endregion

    #region CalculateSnapshot - Zero Total Value Edge Case

    [Fact]
    public void CalculateSnapshot_WithZeroTotalValue_ShouldSetAllocationToZero()
    {
        // Arrange
        var portfolio = new Portfolio
        {
            Id = 1,
            Name = "Empty Portfolio",
            Cash = 0m
        };

        portfolio.AddPosition(new Position
        {
            Ticker = "ZERO",
            Quantity = 10,
            EntryPrice = 0.01m // Must be positive, but price drops to 0
        });

        var currentPrices = new Dictionary<string, decimal>
        {
            ["ZERO"] = 0m // Current price is 0 (stock worthless)
        };

        // Act
        Result<PortfolioSnapshot> result = _service.CalculateSnapshot(portfolio, currentPrices);
        result.IsSuccess.ShouldBeTrue();
        PortfolioSnapshot snapshot = result.Value;

        // Assert
        snapshot.TotalValue.ShouldBe(0m);
        snapshot.Positions[0].AllocationPercentage.ShouldBe(0m);
    }

    #endregion
}
