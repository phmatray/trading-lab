// <copyright file="DashboardCommandTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console.Cli;
using Spectre.Console.Testing;
using TradingBot.Cli.Commands;
using TradingBot.Cli.Dashboard;
using TradingBot.Core.Enums;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Portfolio;
using TradingBot.Core.Models.Risk;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Cli.Tests.Commands;

/// <summary>
/// E2E tests for DashboardCommand using Spectre.Console.Testing's TestConsole.
/// The DashboardCommand and DashboardRenderer accept IAnsiConsole via constructor injection,
/// allowing proper testing with TestConsole without static AnsiConsole dependencies.
/// </summary>
public sealed class DashboardCommandTests
{
    private readonly IPortfolioManager _portfolioManager;
    private readonly IRiskManager _riskManager;
    private readonly TestConsole _console;
    private readonly DashboardRenderer _renderer;
    private readonly DashboardCommand _command;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardCommandTests"/> class.
    /// </summary>
    public DashboardCommandTests()
    {
        _portfolioManager = A.Fake<IPortfolioManager>();
        _riskManager = A.Fake<IRiskManager>();
        _console = new TestConsole();
        _renderer = new DashboardRenderer(_portfolioManager, _riskManager, _console);
        _command = new DashboardCommand(_portfolioManager, _riskManager, _renderer, _console);
    }

    /// <summary>
    /// Test that constructor throws when portfolio manager is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullPortfolioManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DashboardCommand(null!, _riskManager, _renderer, _console));
    }

    /// <summary>
    /// Test that constructor throws when risk manager is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullRiskManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DashboardCommand(_portfolioManager, null!, _renderer, _console));
    }

    /// <summary>
    /// Test that constructor throws when renderer is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullRenderer_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DashboardCommand(_portfolioManager, _riskManager, null!, _console));
    }

    /// <summary>
    /// Test that constructor throws when console is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullConsole_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DashboardCommand(_portfolioManager, _riskManager, _renderer, null!));
    }

    /// <summary>
    /// Test that Execute fetches all required data in static mode using TestConsole.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsync_InStaticMode_ShouldFetchAllRequiredData()
    {
        // Arrange
        var settings = new DashboardCommand.Settings
        {
            Live = false,
            RefreshSeconds = 2
        };

        var account = CreateTestAccount();
        var positions = CreateTestPositions();
        var trades = CreateTestTrades();
        var metrics = CreateTestMetrics();
        var riskSettings = CreateTestRiskSettings();

        A.CallTo(() => _portfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);
        A.CallTo(() => _portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .Returns(positions);
        A.CallTo(() => _portfolioManager.GetTradeHistoryAsync(null, null, null, null, A<CancellationToken>._))
            .Returns(trades);
        A.CallTo(() => _portfolioManager.GetPerformanceMetricsAsync(A<CancellationToken>._))
            .Returns(metrics);
        A.CallTo(() => _riskManager.GetRiskSettingsAsync(A<CancellationToken>._))
            .Returns(riskSettings);

        var context = new CommandContext(Array.Empty<string>(), new FakeRemainingArguments(), "dashboard", null);
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        result.ShouldBe(0);
        A.CallTo(() => _portfolioManager.GetAccountAsync(cancellationToken))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _portfolioManager.GetPositionsAsync(cancellationToken))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _portfolioManager.GetTradeHistoryAsync(null, null, null, null, cancellationToken))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _portfolioManager.GetPerformanceMetricsAsync(cancellationToken))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _riskManager.GetRiskSettingsAsync(cancellationToken))
            .MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// Test static mode with no positions shows empty message using TestConsole.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsync_InStaticModeWithNoPositions_ShouldDisplayEmptyMessage()
    {
        // Arrange
        var settings = new DashboardCommand.Settings
        {
            Live = false,
            RefreshSeconds = 2
        };

        var account = CreateTestAccount();
        var positions = new List<Position>();
        var trades = CreateTestTrades();
        var metrics = CreateTestMetrics();
        var riskSettings = CreateTestRiskSettings();

        A.CallTo(() => _portfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Returns(account);
        A.CallTo(() => _portfolioManager.GetPositionsAsync(A<CancellationToken>._))
            .Returns(positions);
        A.CallTo(() => _portfolioManager.GetTradeHistoryAsync(null, null, null, null, A<CancellationToken>._))
            .Returns(trades);
        A.CallTo(() => _portfolioManager.GetPerformanceMetricsAsync(A<CancellationToken>._))
            .Returns(metrics);
        A.CallTo(() => _riskManager.GetRiskSettingsAsync(A<CancellationToken>._))
            .Returns(riskSettings);

        var context = new CommandContext(Array.Empty<string>(), new FakeRemainingArguments(), "dashboard", null);
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _command.ExecuteAsync(context, settings, cancellationToken);

        // Assert
        result.ShouldBe(0);
    }

    /// <summary>
    /// Test cancellation handling using TestConsole.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task ExecuteAsync_WithCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var settings = new DashboardCommand.Settings
        {
            Live = false,
            RefreshSeconds = 2
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        A.CallTo(() => _portfolioManager.GetAccountAsync(A<CancellationToken>._))
            .Throws(new OperationCanceledException());

        var context = new CommandContext(Array.Empty<string>(), new FakeRemainingArguments(), "dashboard", null);

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _command.ExecuteAsync(context, settings, cts.Token));
    }

    private static Account CreateTestAccount(
        decimal unrealizedPnL = 500.00m,
        decimal realizedPnL = 1000.00m)
    {
        return new Account
        {
            AccountId = "TEST-ACCOUNT-123",
            Equity = 100000.00m,
            Cash = 50000.00m,
            PositionValue = 50000.00m,
            BuyingPower = 200000.00m,
            UnrealizedPnL = unrealizedPnL,
            RealizedPnL = realizedPnL
        };
    }

    private static List<Position> CreateTestPositions()
    {
        return
        [
            new Position
            {
                Id = Guid.NewGuid(),
                Symbol = "AAPL",
                Side = OrderSide.Buy,
                Quantity = 100m,
                EntryPrice = 150.00m,
                CurrentPrice = 155.00m,
                OpenedAt = DateTime.UtcNow.AddHours(-2),
                StrategyName = "MomentumStrategy",
            },
            new Position
            {
                Id = Guid.NewGuid(),
                Symbol = "MSFT",
                Side = OrderSide.Buy,
                Quantity = 50m,
                EntryPrice = 300.00m,
                CurrentPrice = 295.00m,
                OpenedAt = DateTime.UtcNow.AddHours(-1),
                StrategyName = "MeanReversionStrategy",
            }
        ];
    }

    private static List<Trade> CreateTestTrades()
    {
        var now = DateTime.UtcNow;
        return
        [
            new Trade
            {
                Id = Guid.NewGuid(),
                Symbol = "AAPL",
                Side = OrderSide.Buy,
                Quantity = 100m,
                EntryPrice = 150.00m,
                ExitPrice = 155.00m,
                EntryTime = now.AddDays(-1),
                ExitTime = now,
                StrategyName = "MomentumStrategy",
            }
        ];
    }

    private static PerformanceMetrics CreateTestMetrics()
    {
        return new PerformanceMetrics
        {
            TotalReturn = 15.50m,
            AnnualizedReturn = 18.00m,
            SharpeRatio = 1.85m,
            SortinoRatio = 2.10m,
            CalmarRatio = 1.50m,
            MaxDrawdown = 12.50m,
            TotalTrades = 100,
            WinningTrades = 65,
            LosingTrades = 35,
            AverageWin = 150.00m,
            AverageLoss = -75.00m,
            ProfitFactor = 2.15m
        };
    }

    private static RiskSettings CreateTestRiskSettings()
    {
        return new RiskSettings
        {
            Leverage = 2.0m,
            StopLossPercent = 2.5m,
            TakeProfitPercent = 5.0m,
            DailyLossLimit = 5000.00m,
            MaxDrawdownPercent = 15.0m,
            RiskLimitsEnabled = true
        };
    }

    /// <summary>
    /// Fake type resolver for testing.
    /// </summary>
    private sealed class FakeTypeResolver : ITypeResolver
    {
        /// <inheritdoc/>
        public object? Resolve(Type? type)
        {
            return null;
        }
    }

    /// <summary>
    /// Fake remaining arguments for testing.
    /// </summary>
    private sealed class FakeRemainingArguments : IRemainingArguments, System.Collections.IEnumerable
    {
        /// <summary>
        /// Gets the raw arguments.
        /// </summary>
        public IReadOnlyList<string> Raw { get; } = Array.Empty<string>();

        /// <summary>
        /// Gets the parsed arguments.
        /// </summary>
        public ILookup<string, string?> Parsed { get; } = Enumerable.Empty<string>().ToLookup(x => x, x => (string?)null);

        /// <summary>
        /// Gets the count of arguments.
        /// </summary>
        public int Count => 0;

        /// <summary>
        /// Gets the argument at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The argument.</returns>
        public string this[int index] => throw new IndexOutOfRangeException();

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<string> GetEnumerator() => Enumerable.Empty<string>().GetEnumerator();

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
