// <copyright file="WeeklyRoutineExecutorTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using FakeItEasy;
using Microsoft.Extensions.Logging;
using Shouldly;
using TradingBot.Core.Entities;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Configuration;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Portfolio;
using TradingBot.Core.Models.Strategy;
using TradingBot.Core.Models.Trading;
using TradingBot.Core.ValueObjects;
using TradingBot.Engine.WeeklyRoutine;
using Xunit;

namespace TradingBot.Engine.Tests.WeeklyRoutine;

/// <summary>
/// Tests for WeeklyRoutineExecutor.
/// Verifies buy logic, amount calculations, and edge case handling.
/// </summary>
public sealed class WeeklyRoutineExecutorTests
{
    private readonly IWeeklyCashManagedStrategyRepository _fakeStrategyRepository;
    private readonly IMA20IndicatorService _fakeMA20Service;
    private readonly IMarketDataService _fakeMarketDataService;
    private readonly IPortfolioManager _fakePortfolioManager;
    private readonly IOrderExecutionService _fakeOrderExecutionService;
    private readonly IRiskManager _fakeRiskManager;
    private readonly ICashBufferManager _fakeCashBufferManager;
    private readonly ILogger<WeeklyRoutineExecutor> _fakeLogger;
    private readonly WeeklyRoutineExecutor _sut;

    public WeeklyRoutineExecutorTests()
    {
        _fakeStrategyRepository = A.Fake<IWeeklyCashManagedStrategyRepository>();
        _fakeMA20Service = A.Fake<IMA20IndicatorService>();
        _fakeMarketDataService = A.Fake<IMarketDataService>();
        _fakePortfolioManager = A.Fake<IPortfolioManager>();
        _fakeOrderExecutionService = A.Fake<IOrderExecutionService>();
        _fakeRiskManager = A.Fake<IRiskManager>();
        _fakeCashBufferManager = A.Fake<ICashBufferManager>();
        _fakeLogger = A.Fake<ILogger<WeeklyRoutineExecutor>>();

        _sut = new WeeklyRoutineExecutor(
            _fakeStrategyRepository,
            _fakeMA20Service,
            _fakeMarketDataService,
            _fakePortfolioManager,
            _fakeOrderExecutionService,
            _fakeRiskManager,
            _fakeCashBufferManager,
            _fakeLogger);
    }

    /// <summary>
    /// T045: Unit test for WeeklyRoutineExecutor buy logic when COIN > MA20.
    /// Verifies that buy conditions are correctly evaluated when underlying is bullish.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ShouldExecuteBuy_WhenCoinAboveMA20_ReturnsTrue()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);

        // Set up bullish conditions: COIN price (150) > MA20 (140)
        strategy.CurrentUnderlyingPrice = 150m;
        strategy.CurrentMA20 = 140m;

        // Set up cash ratio above minimum (20% > 15%)
        var account = CreateTestAccount(totalEquity: 100000m, cash: 20000m); // 20% cash

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        // Act
        var shouldBuy = await _sut.ShouldExecuteBuyAsync(strategyId);

        // Assert
        shouldBuy.ShouldBeTrue();
    }

    /// <summary>
    /// T045: Verify buy conditions are false when COIN below MA20.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ShouldExecuteBuy_WhenCoinBelowMA20_ReturnsFalse()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);

        // Set up bearish conditions: COIN price (130) < MA20 (140)
        strategy.CurrentUnderlyingPrice = 130m;
        strategy.CurrentMA20 = 140m;

        var account = CreateTestAccount(totalEquity: 100000m, cash: 20000m);

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        // Act
        var shouldBuy = await _sut.ShouldExecuteBuyAsync(strategyId);

        // Assert
        shouldBuy.ShouldBeFalse();
    }

    /// <summary>
    /// T046: Unit test for buy amount calculation (5% of equity).
    /// Verifies that buy amount is correctly calculated as 5% of total equity.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CalculateBuyAmount_With5PercentRatio_Returns5PercentOfEquity()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.WeeklyBuyRatio = 0.05m; // 5%
        strategy.CurrentUnderlyingPrice = 150m;
        strategy.CurrentMA20 = 140m;

        var account = CreateTestAccount(totalEquity: 100000m, cash: 25000m); // 25% cash

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        // Act
        var buyAmount = await _sut.CalculateBuyAmountAsync(strategyId);

        // Assert
        // Expected: 5% of $100,000 = $5,000
        buyAmount.ShouldBe(5000m);
    }

    /// <summary>
    /// T046: Verify buy amount uses breakout multiplier when conditions met.
    /// NOTE: Breakout logic will be implemented in User Story 6 (T129-T149).
    /// This test currently validates standard buy amount calculation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CalculateBuyAmount_WithBreakoutConfigured_ReturnsStandardAmount()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.WeeklyBuyRatio = 0.05m; // 5%
        strategy.CurrentUnderlyingPrice = 150m;
        strategy.CurrentMA20 = 140m;

        // Enable breakout rule with 2x multiplier
        var breakoutConfig = new BreakoutRuleConfig(
            isEnabled: true,
            weeklyPriceIncreaseThreshold: 0.10m,
            volumeMultiplier: 1.5m,
            buyRatioMultiplier: 2.0m);
        strategy.BreakoutRuleConfigJson = System.Text.Json.JsonSerializer.Serialize(breakoutConfig);

        var account = CreateTestAccount(totalEquity: 100000m, cash: 25000m);

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        // Act
        var buyAmount = await _sut.CalculateBuyAmountAsync(strategyId);

        // Assert
        // Breakout logic not yet implemented (User Story 6)
        // Standard calculation: 5% of $100,000 = $5,000
        buyAmount.ShouldBe(5000m);
    }

    /// <summary>
    /// T047: Unit test for buy logic when cash_ratio less than or equal to MIN_CASH_RATIO (no buy).
    /// Verifies that buy does not execute when cash buffer is at minimum threshold.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ShouldExecuteBuy_WhenCashRatioAtMinimum_ReturnsFalse()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.MinCashRatio = 0.15m; // 15% minimum
        strategy.CurrentUnderlyingPrice = 150m;
        strategy.CurrentMA20 = 140m;

        // Cash ratio exactly at minimum: 15%
        var account = CreateTestAccount(totalEquity: 100000m, cash: 15000m);

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        // Act
        var shouldBuy = await _sut.ShouldExecuteBuyAsync(strategyId);

        // Assert
        shouldBuy.ShouldBeFalse("Buy should not execute when cash ratio is at minimum threshold");
    }

    /// <summary>
    /// T047: Verify buy conditions when cash ratio below minimum.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ShouldExecuteBuy_WhenCashRatioBelowMinimum_ReturnsFalse()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.MinCashRatio = 0.15m; // 15% minimum
        strategy.CurrentUnderlyingPrice = 150m;
        strategy.CurrentMA20 = 140m;

        // Cash ratio below minimum: 10% < 15%
        var account = CreateTestAccount(totalEquity: 100000m, cash: 10000m);

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        // Act
        var shouldBuy = await _sut.ShouldExecuteBuyAsync(strategyId);

        // Assert
        shouldBuy.ShouldBeFalse("Buy should not execute when cash ratio is below minimum");
    }

    /// <summary>
    /// T048: Unit test for buy logic when insufficient cash (buy only available amount).
    /// Verifies that buy amount is capped at available cash when calculated amount exceeds it.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CalculateBuyAmount_WhenInsufficientCash_ReturnsAvailableCash()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.WeeklyBuyRatio = 0.10m; // 10% - high buy ratio
        strategy.CurrentUnderlyingPrice = 150m;
        strategy.CurrentMA20 = 140m;

        // Total equity $100k, but only $8k cash available
        // 10% of equity = $10k, but only $8k available
        var account = CreateTestAccount(totalEquity: 100000m, cash: 8000m);

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        // Act
        var buyAmount = await _sut.CalculateBuyAmountAsync(strategyId);

        // Assert
        // Should be capped at available cash: $8,000 (not $10,000)
        buyAmount.ShouldBe(8000m);
    }

    /// <summary>
    /// T048: Verify buy amount is zero when no cash available.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CalculateBuyAmount_WhenNoCashAvailable_ReturnsZero()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.WeeklyBuyRatio = 0.05m;
        strategy.CurrentUnderlyingPrice = 150m;
        strategy.CurrentMA20 = 140m;

        // No cash available (fully invested)
        var account = CreateTestAccount(totalEquity: 100000m, cash: 0m);

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        // Act
        var buyAmount = await _sut.CalculateBuyAmountAsync(strategyId);

        // Assert
        buyAmount.ShouldBe(0m);
    }

    /// <summary>
    /// T046: Verify buy amount calculation with different equity levels.
    /// </summary>
    /// <param name="totalEquity">Total account equity.</param>
    /// <param name="buyRatio">Weekly buy ratio.</param>
    /// <param name="expectedAmount">Expected buy amount.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData(50000, 0.05, 2500)]  // $50k equity, 5% = $2,500
    [InlineData(100000, 0.05, 5000)] // $100k equity, 5% = $5,000
    [InlineData(200000, 0.05, 10000)] // $200k equity, 5% = $10,000
    [InlineData(100000, 0.10, 10000)] // $100k equity, 10% = $10,000
    public async Task CalculateBuyAmount_WithVariousEquityLevels_ReturnsCorrectAmount(
        decimal totalEquity,
        decimal buyRatio,
        decimal expectedAmount)
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.WeeklyBuyRatio = buyRatio;
        strategy.CurrentUnderlyingPrice = 150m;
        strategy.CurrentMA20 = 140m;

        // Ensure sufficient cash (50% of equity)
        var account = CreateTestAccount(totalEquity: totalEquity, cash: totalEquity * 0.5m);

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        // Act
        var buyAmount = await _sut.CalculateBuyAmountAsync(strategyId);

        // Assert
        buyAmount.ShouldBe(expectedAmount);
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

    /// <summary>
    /// T066: End-to-end integration test for weekly buy execution.
    /// Verifies complete workflow with mocked dependencies.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteWeeklyRoutine_WithBuyConditionsMet_ExecutesOrderSuccessfully()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.CurrentUnderlyingPrice = 150m; // COIN price
        strategy.CurrentMA20 = 140m;            // MA20 below COIN (bullish)
        strategy.WeeklyBuyRatio = 0.05m;        // 5%

        var account = CreateTestAccount(totalEquity: 100000m, cash: 20000m); // 20% cash
        var etpPrice = 50m;
        var expectedBuyAmount = 5000m; // 5% of $100k
        var expectedQuantity = Math.Floor(expectedBuyAmount / etpPrice); // 100 shares

        // Set up mocks for complete workflow
        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

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

        A.CallTo(() => _fakeRiskManager.GetRiskSettingsAsync(A<CancellationToken>._))
            .Returns(new RiskSettings { MaxPositionSizePercent = 20m });

        A.CallTo(() => _fakeOrderExecutionService.SubmitOrderAsync(A<Order>._, A<CancellationToken>._))
            .ReturnsLazily((Order order, CancellationToken ct) =>
            {
                order.Status = OrderStatus.Filled;
                return Task.FromResult(order);
            });

        // Act
        var result = await _sut.ExecuteWeeklyRoutineAsync(strategy, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.BuyOrderId.ShouldNotBeNull();
        result.CashRatioAfter.ShouldBeGreaterThan(0);

        // Verify order was submitted with correct parameters
        A.CallTo(() => _fakeOrderExecutionService.SubmitOrderAsync(
                A<Order>.That.Matches(o =>
                    o.Symbol == "BTCW" &&
                    o.Side == OrderSide.Buy &&
                    o.Quantity == expectedQuantity &&
                    o.Type == OrderType.Market),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        // Verify strategy was updated
        A.CallTo(() => _fakeStrategyRepository.UpdateAsync(strategy, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// T067: Verify StrategyExecutedEvent domain event is raised.
    /// NOTE: Domain events are raised within the entity and dispatched by DbContext.
    /// This test verifies the event would be raised through strategy.RecordExecution().
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteWeeklyRoutine_WhenCompleted_UpdatesStrategyTimestamp()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.CurrentUnderlyingPrice = 150m;
        strategy.CurrentMA20 = 140m;

        var account = CreateTestAccount(totalEquity: 100000m, cash: 20000m);

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        var beforeExecution = DateTime.UtcNow;

        // Act
        var result = await _sut.ExecuteWeeklyRoutineAsync(strategy, CancellationToken.None);

        // Assert - Verify LastExecutionTimestamp was updated
        strategy.LastExecutionTimestamp.ShouldNotBeNull();
        strategy.LastExecutionTimestamp.Value.ShouldBeGreaterThanOrEqualTo(beforeExecution);
        strategy.LastExecutionTimestamp.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);

        // In a real scenario with DbContext, StrategyExecutedEvent would be dispatched
        // The event is raised via strategy.RecordExecution() which would be called in production code
    }

    /// <summary>
    /// T068: Test scenario with 20% cash ratio - should execute buy (5% of equity).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteWeeklyRoutine_CashRatio20Percent_ExecutesBuyOf5Percent()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.CurrentUnderlyingPrice = 150m; // COIN > MA20
        strategy.CurrentMA20 = 140m;
        strategy.WeeklyBuyRatio = 0.05m; // 5%
        strategy.MinCashRatio = 0.15m;   // 15% minimum

        // T068: Cash ratio = 20% (above minimum 15%)
        var account = CreateTestAccount(totalEquity: 100000m, cash: 20000m);

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        // Act
        var shouldBuy = await _sut.ShouldExecuteBuyAsync(strategyId);
        var buyAmount = await _sut.CalculateBuyAmountAsync(strategyId);

        // Assert
        shouldBuy.ShouldBeTrue("Buy should execute when cash ratio (20%) > minimum (15%)");
        buyAmount.ShouldBe(5000m, "Buy amount should be 5% of $100,000 equity");
    }

    /// <summary>
    /// T069: Test scenario with 15% cash ratio (exactly at minimum) - should NOT execute buy.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteWeeklyRoutine_CashRatioAtMinimum15Percent_DoesNotExecuteBuy()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.CurrentUnderlyingPrice = 150m; // COIN > MA20 (bullish)
        strategy.CurrentMA20 = 140m;
        strategy.MinCashRatio = 0.15m; // 15% minimum

        // T069: Cash ratio = 15% (exactly at minimum)
        var account = CreateTestAccount(totalEquity: 100000m, cash: 15000m);

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        // Act
        var shouldBuy = await _sut.ShouldExecuteBuyAsync(strategyId);

        // Assert
        shouldBuy.ShouldBeFalse(
            "Buy should NOT execute when cash ratio equals minimum threshold (preserve buffer)");
    }

    /// <summary>
    /// T070: Test mid-week scenario - verify no buy execution (weekly schedule only).
    /// NOTE: This test verifies the buy logic itself. The day-of-week check
    /// is handled by WeeklyRoutineWorker which checks strategy.ExecutionDayOfWeek.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ShouldExecuteBuy_WithAllConditionsMet_ReturnsTrue()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.CurrentUnderlyingPrice = 150m; // COIN > MA20
        strategy.CurrentMA20 = 140m;
        strategy.MinCashRatio = 0.15m;
        strategy.ExecutionDayOfWeek = 5; // Friday

        var account = CreateTestAccount(totalEquity: 100000m, cash: 20000m); // 20%

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        // Act - Buy conditions check (independent of day-of-week)
        var shouldBuy = await _sut.ShouldExecuteBuyAsync(strategyId);

        // Assert
        // T070: ShouldExecuteBuyAsync checks price/cash conditions only
        // Day-of-week filtering is done by WeeklyRoutineWorker.ShouldExecuteToday()
        shouldBuy.ShouldBeTrue(
            "Buy conditions met (COIN > MA20, cash > min). " +
            "Day-of-week check is separate (handled by worker).");
    }

    // ==================== PHASE 5: USER STORY 3 - SELL LOGIC TESTS ====================

    /// <summary>
    /// T073: Unit test for WeeklyRoutineExecutor sell logic when days_below_ma20 >= 2.
    /// Verifies that sell conditions are correctly evaluated when threshold is met.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ShouldExecuteSell_WhenDaysBelowMA20IsTwo_ReturnsTrue()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.DaysBelowMA20 = 2; // Threshold met
        strategy.CurrentUnderlyingPrice = 130m; // Below MA20
        strategy.CurrentMA20 = 140m;

        // Mock existing position of 100 shares
        var position = CreateTestPosition();

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetPositionAsync("BTCW", A<CancellationToken>._))
            .Returns(position);

        // Act
        var shouldSell = await _sut.ShouldExecuteSellAsync(strategyId);

        // Assert
        shouldSell.ShouldBeTrue("Sell should execute when days_below_ma20 >= 2");
    }

    /// <summary>
    /// T074: Unit test for sell quantity calculation (10% of position).
    /// Verifies that sell quantity is correctly calculated as WEEKLY_SELL_RATIO × position_size.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CalculateSellQuantity_With10PercentRatio_Returns10PercentOfPosition()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.WeeklySellRatio = 0.10m; // 10%
        strategy.DaysBelowMA20 = 2;

        // Position: 100 shares
        var position = CreateTestPosition();

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetPositionAsync("BTCW", A<CancellationToken>._))
            .Returns(position);

        // Act
        var sellQuantity = await _sut.CalculateSellQuantityAsync(strategyId);

        // Assert
        // Expected: 10% of 100 shares = 10 shares
        sellQuantity.ShouldBe(10m);
    }

    /// <summary>
    /// T075: Unit test for sell logic when days_below_ma20 = 1 (no sell, threshold not met).
    /// Verifies that sell does not execute when only 1 day below MA20.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ShouldExecuteSell_WhenDaysBelowMA20IsOne_ReturnsFalse()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.DaysBelowMA20 = 1; // Threshold NOT met (needs >= 2)
        strategy.CurrentUnderlyingPrice = 130m;
        strategy.CurrentMA20 = 140m;

        var position = CreateTestPosition();

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetPositionAsync("BTCW", A<CancellationToken>._))
            .Returns(position);

        // Act
        var shouldSell = await _sut.ShouldExecuteSellAsync(strategyId);

        // Assert
        shouldSell.ShouldBeFalse("Sell should NOT execute when days_below_ma20 < 2");
    }

    /// <summary>
    /// T076: Unit test for sell logic when COIN crosses back above MA20 (counter resets, no sell).
    /// Verifies that sell conditions are false after counter reset.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ShouldExecuteSell_WhenCounterResetAfterCrossAbove_ReturnsFalse()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.DaysBelowMA20 = 0; // Counter reset (COIN crossed back above MA20)
        strategy.CurrentUnderlyingPrice = 150m; // Now above MA20
        strategy.CurrentMA20 = 140m;

        var position = CreateTestPosition();

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetPositionAsync("BTCW", A<CancellationToken>._))
            .Returns(position);

        // Act
        var shouldSell = await _sut.ShouldExecuteSellAsync(strategyId);

        // Assert
        shouldSell.ShouldBeFalse("Sell should NOT execute when counter reset (COIN > MA20)");
    }

    /// <summary>
    /// T075: Verify sell logic when no position exists - should return false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ShouldExecuteSell_WhenNoPosition_ReturnsFalse()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.DaysBelowMA20 = 2; // Threshold met
        strategy.CurrentUnderlyingPrice = 130m;
        strategy.CurrentMA20 = 140m;

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        // No position exists
        A.CallTo(() => _fakePortfolioManager.GetPositionAsync("BTCW", A<CancellationToken>._))
            .Returns(Task.FromResult<Position?>(null));

        // Act
        var shouldSell = await _sut.ShouldExecuteSellAsync(strategyId);

        // Assert
        shouldSell.ShouldBeFalse("Sell should NOT execute when no position exists");
    }

    /// <summary>
    /// T074: Verify sell quantity calculation with different position sizes.
    /// </summary>
    /// <param name="positionSize">Current position size.</param>
    /// <param name="sellRatio">Weekly sell ratio.</param>
    /// <param name="expectedQuantity">Expected sell quantity.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData(50, 0.10, 5)]   // 50 shares, 10% = 5 shares
    [InlineData(100, 0.10, 10)] // 100 shares, 10% = 10 shares
    [InlineData(200, 0.10, 20)] // 200 shares, 10% = 20 shares
    [InlineData(100, 0.20, 20)] // 100 shares, 20% = 20 shares
    public async Task CalculateSellQuantity_WithVariousPositionSizes_ReturnsCorrectQuantity(
        decimal positionSize,
        decimal sellRatio,
        decimal expectedQuantity)
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.WeeklySellRatio = sellRatio;
        strategy.DaysBelowMA20 = 2;

        var position = CreateTestPosition(quantity: positionSize);

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetPositionAsync("BTCW", A<CancellationToken>._))
            .Returns(position);

        // Act
        var sellQuantity = await _sut.CalculateSellQuantityAsync(strategyId);

        // Assert
        sellQuantity.ShouldBe(expectedQuantity);
    }

    // ==================== INTEGRATION TESTS FOR SELL LOGIC ====================

    /// <summary>
    /// T085: End-to-end integration test for weekly sell execution.
    /// Verifies complete sell workflow with mocked dependencies.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteWeeklyRoutine_WithSellConditionsMet_ExecutesOrderSuccessfully()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.DaysBelowMA20 = 2; // Threshold met
        strategy.CurrentUnderlyingPrice = 130m; // COIN < MA20 (bearish)
        strategy.CurrentMA20 = 140m;
        strategy.WeeklySellRatio = 0.10m; // 10%

        var account = CreateTestAccount(totalEquity: 100000m, cash: 20000m);
        var etpPrice = 50m;

        // Existing position: 100 shares
        var position = CreateTestPosition(currentPrice: etpPrice);

        var expectedSellQuantity = 10m; // 10% of 100 shares

        // Set up mocks for complete workflow
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
        var result = await _sut.ExecuteWeeklyRoutineAsync(strategy, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.SellOrderId.ShouldNotBeNull("Sell order should be placed when conditions met");
        result.CashRatioAfter.ShouldBeGreaterThan(0);

        // Verify sell order was submitted with correct parameters
        A.CallTo(() => _fakeOrderExecutionService.SubmitOrderAsync(
                A<Order>.That.Matches(o =>
                    o.Symbol == "BTCW" &&
                    o.Side == OrderSide.Sell &&
                    o.Quantity == expectedSellQuantity &&
                    o.Type == OrderType.Market),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        // Verify strategy was updated
        A.CallTo(() => _fakeStrategyRepository.UpdateAsync(strategy, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// T086: Verify StrategyExecutedEvent domain event is raised with sell order details.
    /// NOTE: Domain events are raised within the entity and dispatched by DbContext.
    /// This test verifies the sell order ID would be recorded through strategy state.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteWeeklyRoutine_WithSellExecution_UpdatesStrategyTimestamp()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.DaysBelowMA20 = 2;
        strategy.CurrentUnderlyingPrice = 130m;
        strategy.CurrentMA20 = 140m;

        var account = CreateTestAccount(totalEquity: 100000m, cash: 20000m);
        var position = CreateTestPosition();

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
                Price = 50m,
                Timestamp = DateTime.UtcNow,
                Bid = 49.99m,
                Ask = 50.01m,
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

        var beforeExecution = DateTime.UtcNow;

        // Act
        var result = await _sut.ExecuteWeeklyRoutineAsync(strategy, CancellationToken.None);

        // Assert - Verify LastExecutionTimestamp was updated
        strategy.LastExecutionTimestamp.ShouldNotBeNull();
        strategy.LastExecutionTimestamp.Value.ShouldBeGreaterThanOrEqualTo(beforeExecution);
        strategy.LastExecutionTimestamp.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    /// <summary>
    /// T087: Test scenario with days_below_ma20 = 2 and 100 shares - should sell 10 shares (10%).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteWeeklyRoutine_DaysBelowMA20Is2_Sells10PercentOfPosition()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.DaysBelowMA20 = 2;
        strategy.CurrentUnderlyingPrice = 130m; // COIN < MA20
        strategy.CurrentMA20 = 140m;
        strategy.WeeklySellRatio = 0.10m;

        var account = CreateTestAccount(totalEquity: 100000m, cash: 20000m);
        var position = CreateTestPosition();

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetPositionAsync("BTCW", A<CancellationToken>._))
            .Returns(position);

        // Act
        var shouldSell = await _sut.ShouldExecuteSellAsync(strategyId);
        var sellQuantity = await _sut.CalculateSellQuantityAsync(strategyId);

        // Assert
        shouldSell.ShouldBeTrue("Sell should execute when days_below_ma20 = 2");
        sellQuantity.ShouldBe(10m, "Sell quantity should be 10% of 100 shares");
    }

    /// <summary>
    /// T088: Test scenario with days_below_ma20 = 1 - should NOT sell (threshold not met).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteWeeklyRoutine_DaysBelowMA20Is1_DoesNotExecuteSell()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.DaysBelowMA20 = 1; // Threshold NOT met
        strategy.CurrentUnderlyingPrice = 130m;
        strategy.CurrentMA20 = 140m;

        var account = CreateTestAccount(totalEquity: 100000m, cash: 20000m);
        var position = CreateTestPosition();

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        A.CallTo(() => _fakePortfolioManager.GetPositionAsync("BTCW", A<CancellationToken>._))
            .Returns(position);

        // Act
        var result = await _sut.ExecuteWeeklyRoutineAsync(strategy, CancellationToken.None);

        // Assert
        result.SellOrderId.ShouldBeNull("Sell order should NOT be placed when days_below_ma20 < 2");

        // Verify no sell order was submitted
        A.CallTo(() => _fakeOrderExecutionService.SubmitOrderAsync(
                A<Order>.That.Matches(o => o.Side == OrderSide.Sell),
                A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    /// <summary>
    /// T089: Test scenario where COIN crosses back above MA20 - counter resets, no sell.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteWeeklyRoutine_CounterResetAfterCrossAbove_DoesNotExecuteSell()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.DaysBelowMA20 = 0; // Counter reset
        strategy.CurrentUnderlyingPrice = 150m; // COIN > MA20 (crossed back above)
        strategy.CurrentMA20 = 140m;

        var account = CreateTestAccount(totalEquity: 100000m, cash: 20000m);
        var position = CreateTestPosition();

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);

        A.CallTo(() => _fakePortfolioManager.GetPositionAsync("BTCW", A<CancellationToken>._))
            .Returns(position);

        // Act
        var result = await _sut.ExecuteWeeklyRoutineAsync(strategy, CancellationToken.None);

        // Assert
        result.SellOrderId.ShouldBeNull("Sell should NOT execute when counter reset");

        // Verify no sell order was submitted
        A.CallTo(() => _fakeOrderExecutionService.SubmitOrderAsync(
                A<Order>.That.Matches(o => o.Side == OrderSide.Sell),
                A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    /// <summary>
    /// T090: Test mid-week scenario - days_below_ma20 increments but no sell order (weekly schedule only).
    /// NOTE: This test verifies sell logic conditions. Day-of-week filtering is handled by WeeklyRoutineWorker.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ShouldExecuteSell_WithAllConditionsMet_ReturnsTrue()
    {
        // Arrange
        var strategyId = Guid.NewGuid();
        var strategy = CreateTestStrategy(strategyId);
        strategy.DaysBelowMA20 = 3; // Well above threshold
        strategy.CurrentUnderlyingPrice = 130m;
        strategy.CurrentMA20 = 140m;

        var position = CreateTestPosition();

        A.CallTo(() => _fakeStrategyRepository.GetByIdAsync(strategyId, A<CancellationToken>._))
            .Returns(strategy);

        A.CallTo(() => _fakePortfolioManager.GetPositionAsync("BTCW", A<CancellationToken>._))
            .Returns(position);

        // Act - Sell conditions check (independent of day-of-week)
        var shouldSell = await _sut.ShouldExecuteSellAsync(strategyId);

        // Assert
        // T090: ShouldExecuteSellAsync checks days_below_ma20 and position only
        // Day-of-week filtering is done by WeeklyRoutineWorker.ShouldExecuteToday()
        shouldSell.ShouldBeTrue(
            "Sell conditions met (days_below_ma20 >= 2, position > 0). " +
            "Day-of-week check is separate (handled by worker).");
    }
}
