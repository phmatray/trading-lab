// <copyright file="CashBufferManagerTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using FakeItEasy;
using Microsoft.Extensions.Logging;
using Shouldly;
using TradingBot.Core.Entities;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Portfolio;
using TradingBot.Core.Models.Strategy;
using TradingBot.Core.Models.Trading;
using TradingBot.Engine.WeeklyRoutine;
using Xunit;

namespace TradingBot.Engine.Tests.WeeklyRoutine;

/// <summary>
/// Tests for CashBufferManager.
/// Verifies cash buffer rebalancing logic and edge cases.
/// </summary>
public sealed class CashBufferManagerTests
{
    private readonly IWeeklyCashManagedStrategyRepository _fakeStrategyRepository;
    private readonly IMarketDataService _fakeMarketDataService;
    private readonly IPortfolioManager _fakePortfolioManager;
    private readonly IOrderExecutionService _fakeOrderExecutionService;
    private readonly IRiskManager _fakeRiskManager;
    private readonly ILogger<CashBufferManager> _fakeLogger;
    private readonly CashBufferManager _sut;

    public CashBufferManagerTests()
    {
        _fakeStrategyRepository = A.Fake<IWeeklyCashManagedStrategyRepository>();
        _fakeMarketDataService = A.Fake<IMarketDataService>();
        _fakePortfolioManager = A.Fake<IPortfolioManager>();
        _fakeOrderExecutionService = A.Fake<IOrderExecutionService>();
        _fakeRiskManager = A.Fake<IRiskManager>();
        _fakeLogger = A.Fake<ILogger<CashBufferManager>>();

        _sut = new CashBufferManager(
            _fakeStrategyRepository,
            _fakeMarketDataService,
            _fakePortfolioManager,
            _fakeOrderExecutionService,
            _fakeRiskManager,
            _fakeLogger);
    }

    /// <summary>
    /// T091: Unit test for CashBufferManager when cash_ratio &lt; MIN_CASH_RATIO.
    /// Verifies that system sells position to rebuild cash buffer.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AdjustCashBuffer_WhenCashRatioBelowMinimum_ExecutesSell()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.MinCashRatio = 0.15m; // 15% minimum
        strategy.WeeklySellRatio = 0.10m; // 10% sell ratio

        // Cash ratio = 12% (below 15% minimum)
        var account = CreateTestAccount(totalEquity: 100000m, cash: 12000m);
        var position = CreateTestPosition();
        var etpPrice = 50m;

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        A.CallTo(() => _fakePortfolioManager.GetPositionAsync("BTCW", A<CancellationToken>._))
            .Returns(position);

        A.CallTo(() => _fakeMarketDataService.GetQuoteAsync("BTCW", A<CancellationToken>._))
            .Returns(new Quote
            {
                Symbol = "BTCW",
                Price = etpPrice,
                Timestamp = DateTime.UtcNow,
                Bid = etpPrice - 0.01m,
                Ask = etpPrice + 0.01m,
                Volume = 1000000,
                Change = 0m,
                ChangePercent = 0m,
            });

        A.CallTo(() => _fakeOrderExecutionService.SubmitOrderAsync(A<Order>._, A<CancellationToken>._))
            .ReturnsLazily((Order order, CancellationToken ct) =>
            {
                order.Status = OrderStatus.Filled;
                return Task.FromResult(order);
            });

        // Act
        var result = await _sut.AdjustCashBufferAsync(strategyId, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Adjusted.ShouldBeTrue("Cash buffer should be adjusted when below minimum");
        result.OrderId.ShouldNotBeNull("Sell order should be placed to rebuild buffer");
        result.Action.ShouldBe("Sell");
        result.CashRatioBefore.ShouldBe(0.12m); // 12%
        result.CashRatioAfter.ShouldBeGreaterThan(0.12m, "Cash ratio should increase after sell");

        // Verify sell order was submitted (10% of position)
        A.CallTo(() => _fakeOrderExecutionService.SubmitOrderAsync(
                A<Order>.That.Matches(o =>
                    o.Symbol == "BTCW" &&
                    o.Side == OrderSide.Sell &&
                    o.Quantity == 10m && // 10% of 100 shares
                    o.Type == OrderType.Market),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// T092: Unit test for CashBufferManager when cash_ratio &gt; MAX_CASH_RATIO.
    /// Verifies that system buys position to reduce excess cash (only if COIN > MA20).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AdjustCashBuffer_WhenCashRatioAboveMaximumAndBullish_ExecutesBuy()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.MaxCashRatio = 0.25m; // 25% maximum
        strategy.CurrentUnderlyingPrice = 150m; // COIN > MA20 (bullish)
        strategy.CurrentMA20 = 140m;

        // Cash ratio = 30% (above 25% maximum)
        var account = CreateTestAccount(totalEquity: 100000m, cash: 30000m);
        var position = CreateTestPosition();
        var etpPrice = 50m;

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        A.CallTo(() => _fakePortfolioManager.GetPositionAsync("BTCW", A<CancellationToken>._))
            .Returns(position);

        A.CallTo(() => _fakeMarketDataService.GetQuoteAsync("BTCW", A<CancellationToken>._))
            .Returns(new Quote
            {
                Symbol = "BTCW",
                Price = etpPrice,
                Timestamp = DateTime.UtcNow,
                Bid = etpPrice - 0.01m,
                Ask = etpPrice + 0.01m,
                Volume = 1000000,
                Change = 0m,
                ChangePercent = 0m,
            });

        A.CallTo(() => _fakeOrderExecutionService.SubmitOrderAsync(A<Order>._, A<CancellationToken>._))
            .ReturnsLazily((Order order, CancellationToken ct) =>
            {
                order.Status = OrderStatus.Filled;
                return Task.FromResult(order);
            });

        // Act
        var result = await _sut.AdjustCashBufferAsync(strategyId, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Adjusted.ShouldBeTrue("Cash buffer should be adjusted when above maximum");
        result.OrderId.ShouldNotBeNull("Buy order should be placed to use excess cash");
        result.Action.ShouldBe("Buy");
        result.CashRatioBefore.ShouldBe(0.30m); // 30%
        result.CashRatioAfter.ShouldBeLessThan(0.30m, "Cash ratio should decrease after buy");

        // Verify buy order was submitted with excess cash
        A.CallTo(() => _fakeOrderExecutionService.SubmitOrderAsync(
                A<Order>.That.Matches(o =>
                    o.Symbol == "BTCW" &&
                    o.Side == OrderSide.Buy &&
                    o.Type == OrderType.Market),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// T093: Unit test for CashBufferManager when cash_ratio within range (no action).
    /// Verifies that no adjustment is made when cash ratio is healthy.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AdjustCashBuffer_WhenCashRatioWithinRange_NoAdjustment()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.MinCashRatio = 0.15m; // 15% minimum
        strategy.MaxCashRatio = 0.25m; // 25% maximum

        // Cash ratio = 20% (within 15-25% range)
        var account = CreateTestAccount(totalEquity: 100000m, cash: 20000m);

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        // Act
        var result = await _sut.AdjustCashBufferAsync(strategyId, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Adjusted.ShouldBeFalse("No adjustment should be made when cash ratio is healthy");
        result.OrderId.ShouldBeNull("No order should be placed");
        result.Action.ShouldBe("None");
        result.CashRatioBefore.ShouldBe(0.20m); // 20%
        result.CashRatioAfter.ShouldBe(0.20m, "Cash ratio should remain unchanged");

        // Verify no orders were submitted
        A.CallTo(() => _fakeOrderExecutionService.SubmitOrderAsync(A<Order>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    /// <summary>
    /// T094: Unit test for CashBufferManager respecting COIN &gt; MA20 condition for excess cash buys.
    /// Verifies that buy is NOT executed when cash is high but COIN &lt; MA20 (bearish).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AdjustCashBuffer_WhenCashHighButBearish_DoesNotBuy()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.MaxCashRatio = 0.25m; // 25% maximum
        strategy.CurrentUnderlyingPrice = 130m; // COIN < MA20 (bearish)
        strategy.CurrentMA20 = 140m;

        // Cash ratio = 30% (above 25% maximum)
        var account = CreateTestAccount(totalEquity: 100000m, cash: 30000m);

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        // Act
        var result = await _sut.AdjustCashBufferAsync(strategyId, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Adjusted.ShouldBeFalse(
            "No buy adjustment should be made when bearish (only buys if bullish)");
        result.OrderId.ShouldBeNull("No order should be placed");
        result.Action.ShouldBe("None");
        result.CashRatioBefore.ShouldBe(0.30m); // 30%
        result.CashRatioAfter.ShouldBe(0.30m, "Cash ratio should remain unchanged");

        // Verify no buy order was submitted
        A.CallTo(() => _fakeOrderExecutionService.SubmitOrderAsync(
                A<Order>.That.Matches(o => o.Side == OrderSide.Buy),
                A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    private static WeeklyCashManagedStrategy CreateTestStrategy(Guid strategyId)
    {
        return new WeeklyCashManagedStrategy
        {
            Id = strategyId,
            Name = "Test Strategy",
            EtpSymbol = "BTCW",
            UnderlyingSymbol = "COIN",
            IsEnabled = true,
            MinCashRatio = 0.15m,
            MaxCashRatio = 0.25m,
            WeeklyBuyRatio = 0.05m,
            WeeklySellRatio = 0.10m,
            ExecutionDayOfWeek = 5, // Friday
            DaysBelowMA20 = 0,
            CreatedAt = DateTime.UtcNow,
            CurrentUnderlyingPrice = null,
            CurrentEtpPrice = null,
            CurrentMA20 = null,
        };
    }

    private static Account CreateTestAccount(decimal totalEquity, decimal cash)
    {
        return new Account
        {
            AccountId = "TEST-ACCOUNT",
            Cash = cash,
            Equity = totalEquity,
        };
    }

    private static Position CreateTestPosition(string symbol = "BTCW", decimal quantity = 100, decimal entryPrice = 45m, decimal currentPrice = 50m)
    {
        return new Position
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            Side = OrderSide.Buy,
            Quantity = quantity,
            EntryPrice = entryPrice,
            CurrentPrice = currentPrice,
            OpenedAt = DateTime.UtcNow.AddDays(-7),
            StrategyName = "Test Strategy",
        };
    }
}
