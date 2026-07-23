using FakeItEasy;
using Microsoft.Extensions.Logging;
using Shouldly;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Services;
using TradingStrat.Application.Tests.TestDoubles;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;
using DomainPythonValidationResult = TradingStrat.Domain.Services.PythonValidationResult;

namespace TradingStrat.Application.Tests.UseCases;

public class DryRunPythonStrategyUseCaseTests
{
    private readonly IPythonExecutor _fakePythonExecutor;
    private readonly InMemoryHistoricalDataRepository _historicalDataPort;
    private readonly BacktestEngine _backtestEngine;
    private readonly IIndicatorCalculator _fakeIndicatorCalculator;
    private readonly ILogger<DryRunPythonStrategyUseCase> _fakeLogger;
    private readonly DryRunPythonStrategyUseCase _useCase;

    public DryRunPythonStrategyUseCaseTests()
    {
        _fakePythonExecutor = A.Fake<IPythonExecutor>();
        _historicalDataPort = new InMemoryHistoricalDataRepository();
        _fakeIndicatorCalculator = A.Fake<IIndicatorCalculator>();
        var performanceCalculator = new PerformanceCalculator();
        _backtestEngine = new BacktestEngine(_historicalDataPort, performanceCalculator);
        _fakeLogger = A.Fake<ILogger<DryRunPythonStrategyUseCase>>();

        _useCase = new DryRunPythonStrategyUseCase(
            _fakePythonExecutor,
            _historicalDataPort,
            _backtestEngine,
            _fakeIndicatorCalculator,
            _fakeLogger);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCode_ReturnsSuccessfulDryRun()
    {
        // Arrange
        SeedTestData();

        const string validCode = @"
def generate_signal(index, price, cash, position):
    if index % 10 == 0 and position == 0:
        return {'action': 'buy', 'quantity': 1, 'reason': 'Test buy'}
    if index % 20 == 0 and position > 0:
        return {'action': 'sell', 'quantity': position, 'reason': 'Test sell'}
    return {'action': 'hold', 'quantity': 0, 'reason': 'Hold'}
";

        var command = new DryRunPythonStrategyCommand(validCode, "TEST", InitialCash: 10000m);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(true, new List<string>())));

        // Configure fake executor to return buy/sell/hold signals
        A.CallTo(() => _fakePythonExecutor.ExecuteInitializeAsync(
                A<string>._, A<IReadOnlyList<HistoricalPrice>>._, A<CancellationToken>._))
            .Returns(Task.CompletedTask);

        A.CallTo(() => _fakePythonExecutor.ExecuteGenerateSignalAsync(
                A<int>._, A<decimal>._, A<decimal>._, A<int>._, A<CancellationToken>._))
            .Returns(Task.FromResult(new PythonSignalResult("hold", 0, "Hold")));

        // Act
        Result<DryRunResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsValid.ShouldBeTrue();
        result.Value.ValidationErrors.ShouldBeEmpty();
        result.Value.TotalTrades.ShouldNotBeNull();
        result.Value.FinalEquity.ShouldNotBeNull();
        result.Value.TotalReturn.ShouldNotBeNull();
        result.Value.SharpeRatio.ShouldNotBeNull();

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidSyntax_ReturnsValidationErrors()
    {
        // Arrange
        const string invalidCode = @"
def generate_signal(index, price, cash, position)  # Missing colon
    return {'action': 'hold'}
";

        var command = new DryRunPythonStrategyCommand(invalidCode, "TEST", InitialCash: 10000m);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(
                false,
                new List<string> { "SyntaxError: invalid syntax at line 2" })));

        // Act
        Result<DryRunResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsValid.ShouldBeFalse();
        result.Value.ValidationErrors.Count.ShouldBe(1);
        result.Value.ValidationErrors[0].ShouldContain("SyntaxError");
        result.Value.TotalTrades.ShouldBeNull();
        result.Value.FinalEquity.ShouldBeNull();
        result.Value.TotalReturn.ShouldBeNull();
        result.Value.SharpeRatio.ShouldBeNull();

        // Should not proceed to backtest if validation fails
        A.CallTo(() => _fakePythonExecutor.ExecuteInitializeAsync(
                A<string>._, A<IReadOnlyList<HistoricalPrice>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoHistoricalData_ReturnsError()
    {
        // Arrange - No data seeded
        const string validCode = @"
def generate_signal(index, price, cash, position):
    return {'action': 'hold', 'quantity': 0, 'reason': 'Hold'}
";

        var command = new DryRunPythonStrategyCommand(validCode, "NODATA", InitialCash: 10000m);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(true, new List<string>())));

        // Act
        Result<DryRunResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsValid.ShouldBeFalse();
        result.Value.ValidationErrors.Count.ShouldBe(1);
        result.Value.ValidationErrors[0].ShouldContain("No historical data found");
        result.Value.ValidationErrors[0].ShouldContain("NODATA");
        result.Value.TotalTrades.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomDateRange_UsesSpecifiedDates()
    {
        // Arrange
        SeedTestData();

        const string validCode = @"
def generate_signal(index, price, cash, position):
    return {'action': 'hold', 'quantity': 0, 'reason': 'Hold'}
";

        var startDate = new DateTime(2024, 1, 15);
        var endDate = new DateTime(2024, 2, 15);

        var command = new DryRunPythonStrategyCommand(
            validCode,
            "TEST",
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 10000m);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(true, new List<string>())));

        A.CallTo(() => _fakePythonExecutor.ExecuteInitializeAsync(
                A<string>._, A<IReadOnlyList<HistoricalPrice>>._, A<CancellationToken>._))
            .Returns(Task.CompletedTask);

        A.CallTo(() => _fakePythonExecutor.ExecuteGenerateSignalAsync(
                A<int>._, A<decimal>._, A<decimal>._, A<int>._, A<CancellationToken>._))
            .Returns(Task.FromResult(new PythonSignalResult("hold", 0, "Hold")));

        // Act
        Result<DryRunResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultDateRange_UsesTwoYears()
    {
        // Arrange
        SeedTestData();

        const string validCode = @"
def generate_signal(index, price, cash, position):
    return {'action': 'hold', 'quantity': 0, 'reason': 'Hold'}
";

        var command = new DryRunPythonStrategyCommand(validCode, "TEST", InitialCash: 10000m);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(true, new List<string>())));

        A.CallTo(() => _fakePythonExecutor.ExecuteInitializeAsync(
                A<string>._, A<IReadOnlyList<HistoricalPrice>>._, A<CancellationToken>._))
            .Returns(Task.CompletedTask);

        A.CallTo(() => _fakePythonExecutor.ExecuteGenerateSignalAsync(
                A<int>._, A<decimal>._, A<decimal>._, A<int>._, A<CancellationToken>._))
            .Returns(Task.FromResult(new PythonSignalResult("hold", 0, "Hold")));

        // Act
        Result<DryRunResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPythonExecutionFails_ReturnsExecutionError()
    {
        // Arrange
        SeedTestData();

        const string validCode = @"
def generate_signal(index, price, cash, position):
    raise Exception('Runtime error')
";

        var command = new DryRunPythonStrategyCommand(validCode, "TEST", InitialCash: 10000m);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(true, new List<string>())));

        A.CallTo(() => _fakePythonExecutor.ExecuteInitializeAsync(
                A<string>._, A<IReadOnlyList<HistoricalPrice>>._, A<CancellationToken>._))
            .Throws(new PythonExecutionException("Runtime error in initialize()"));

        // Act
        Result<DryRunResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsValid.ShouldBeFalse();
        result.Value.ValidationErrors.Count.ShouldBe(1);
        result.Value.ValidationErrors[0].ShouldContain("Python execution error");
        result.Value.ValidationErrors[0].ShouldContain("Runtime error");
    }

    [Fact]
    public async Task ExecuteAsync_WhenBacktestEngineThrows_ReturnsFailure()
    {
        // Arrange
        SeedTestData();

        const string validCode = @"
def generate_signal(index, price, cash, position):
    return {'action': 'hold', 'quantity': 0, 'reason': 'Hold'}
";

        var command = new DryRunPythonStrategyCommand(validCode, "TEST", InitialCash: 10000m);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(true, new List<string>())));

        A.CallTo(() => _fakePythonExecutor.ExecuteInitializeAsync(
                A<string>._, A<IReadOnlyList<HistoricalPrice>>._, A<CancellationToken>._))
            .Throws(new InvalidOperationException("Backtest engine error"));

        // Act
        Result<DryRunResult> result = await _useCase.ExecuteAsync(command);

        // Assert - When Python executor throws non-Python exception, returns Failure Result
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Dry run failed"));
        result.Errors.ShouldContain(e => e.Message.Contains("Backtest engine error"));
        result.Errors.ShouldContain(e => e.Code == "STRATEGY_EXECUTION_FAILED");
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexStrategy_ReturnsMetrics()
    {
        // Arrange
        SeedTestData();

        const string complexCode = @"
import talib

sma_20 = None

def initialize(prices):
    global sma_20
    sma_20 = talib.SMA(prices['close'], timeperiod=20)

def generate_signal(index, price, cash, position):
    if index < 20:
        return {'action': 'hold', 'quantity': 0, 'reason': 'Warming up'}

    if price > sma_20[index] and position == 0:
        quantity = int((cash * 0.95) / price)
        return {'action': 'buy', 'quantity': quantity, 'reason': 'Above SMA'}

    if price < sma_20[index] and position > 0:
        return {'action': 'sell', 'quantity': position, 'reason': 'Below SMA'}

    return {'action': 'hold', 'quantity': 0, 'reason': 'No signal'}
";

        var command = new DryRunPythonStrategyCommand(complexCode, "TEST", InitialCash: 10000m);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(true, new List<string>())));

        A.CallTo(() => _fakePythonExecutor.ExecuteInitializeAsync(
                A<string>._, A<IReadOnlyList<HistoricalPrice>>._, A<CancellationToken>._))
            .Returns(Task.CompletedTask);

        A.CallTo(() => _fakePythonExecutor.ExecuteGenerateSignalAsync(
                A<int>._, A<decimal>._, A<decimal>._, A<int>._, A<CancellationToken>._))
            .Returns(Task.FromResult(new PythonSignalResult("hold", 0, "No signal")));

        // Act
        Result<DryRunResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsValid.ShouldBeTrue();
        result.Value.TotalTrades.ShouldNotBeNull();
        result.Value.FinalEquity.ShouldNotBeNull();
        result.Value.TotalReturn.ShouldNotBeNull();
        result.Value.SharpeRatio.ShouldNotBeNull();
        result.Value.FinalEquity!.Value.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithMaliciousCode_ReturnsSecurityError()
    {
        // Arrange
        const string maliciousCode = @"
import os

def generate_signal(index, price, cash, position):
    os.system('rm -rf /')
    return {'action': 'hold', 'quantity': 0, 'reason': 'Evil'}
";

        var command = new DryRunPythonStrategyCommand(maliciousCode, "TEST", InitialCash: 10000m);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(
                false,
                new List<string> { "SecurityError: Import 'os' is not allowed" })));

        // Act
        Result<DryRunResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsValid.ShouldBeFalse();
        result.Value.ValidationErrors.ShouldContain(e => e.Contains("SecurityError"));
        result.Value.TotalTrades.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithEmptyCode_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var ex = Should.Throw<ArgumentException>(() => new DryRunPythonStrategyCommand(string.Empty, "TEST", InitialCash: 10000m));
        ex.Message.ShouldContain("PythonCode cannot be null, empty, or whitespace");
    }

    private void SeedTestData()
    {
        var data = new List<HistoricalPrice>();
        var baseDate = new DateTime(2024, 1, 1);

        for (int i = 0; i < 100; i++)
        {
            data.Add(new HistoricalPrice
            {
                Ticker = "TEST",
                DateTime = baseDate.AddDays(i),
                Open = 100m + i * 0.5m,
                High = 100m + i * 0.5m + 1m,
                Low = 100m + i * 0.5m - 1m,
                Close = 100m + i * 0.5m,
                AdjustedClose = 100m + i * 0.5m,
                Volume = 1000000
            });
        }

        _historicalDataPort.SeedData("TEST", TimeFrameUnit.D1, data);
    }
}
