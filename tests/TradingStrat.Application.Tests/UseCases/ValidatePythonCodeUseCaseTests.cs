using FakeItEasy;
using Microsoft.Extensions.Logging;
using Shouldly;
using TradingStrat.Application.Commands;
using TradingStrat.Application.UseCases;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Services;
using AppPythonValidationResult = TradingStrat.Application.Commands.PythonValidationResult;
using DomainPythonValidationResult = TradingStrat.Domain.Services.PythonValidationResult;

namespace TradingStrat.Application.Tests.UseCases;

public class ValidatePythonCodeUseCaseTests
{
    private readonly IPythonExecutor _fakePythonExecutor;
    private readonly ILogger<ValidatePythonCodeUseCase> _fakeLogger;
    private readonly ValidatePythonCodeUseCase _useCase;

    public ValidatePythonCodeUseCaseTests()
    {
        _fakePythonExecutor = A.Fake<IPythonExecutor>();
        _fakeLogger = A.Fake<ILogger<ValidatePythonCodeUseCase>>();
        _useCase = new ValidatePythonCodeUseCase(_fakePythonExecutor, _fakeLogger);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidPythonCode_ReturnsSuccess()
    {
        // Arrange
        const string validCode = @"
def generate_signal(index, price, cash, position):
    return {'action': 'hold', 'quantity': 0, 'reason': 'Test'}
";
        var command = new ValidatePythonCodeCommand(validCode);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(true, new List<string>())));

        // Act
        Result<AppPythonValidationResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsValid.ShouldBeTrue();
        result.Value.Errors.ShouldBeEmpty();

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithSyntaxError_ReturnsInvalid()
    {
        // Arrange
        const string invalidCode = @"
def generate_signal(index, price, cash, position)  # Missing colon
    return {'action': 'hold'}
";
        var command = new ValidatePythonCodeCommand(invalidCode);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(
                false,
                new List<string> { "SyntaxError: invalid syntax at line 2" })));

        // Act
        Result<AppPythonValidationResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsValid.ShouldBeFalse();
        result.Value.Errors.Count.ShouldBe(1);
        result.Value.Errors[0].ShouldContain("SyntaxError");
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingGenerateSignal_ReturnsInvalid()
    {
        // Arrange
        const string codeWithoutRequiredFunction = @"
def initialize(prices):
    pass
";
        var command = new ValidatePythonCodeCommand(codeWithoutRequiredFunction);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(
                false,
                new List<string> { "Missing required function: generate_signal" })));

        // Act
        Result<AppPythonValidationResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsValid.ShouldBeFalse();
        result.Value.Errors.ShouldContain(e => e.Contains("generate_signal"));
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        const string badCode = @"
def generate_signal(index, price, cash, position)  # Missing colon
    import os  # Disallowed import
    return invalid_dict  # Undefined variable
";
        var command = new ValidatePythonCodeCommand(badCode);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(
                false,
                new List<string>
                {
                    "SyntaxError: invalid syntax at line 2",
                    "SecurityError: Import 'os' is not allowed",
                    "NameError: name 'invalid_dict' is not defined"
                })));

        // Act
        Result<AppPythonValidationResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsValid.ShouldBeFalse();
        result.Value.Errors.Count.ShouldBe(3);
        result.Value.Errors.ShouldContain(e => e.Contains("SyntaxError"));
        result.Value.Errors.ShouldContain(e => e.Contains("SecurityError"));
        result.Value.Errors.ShouldContain(e => e.Contains("NameError"));
    }

    [Fact]
    public void Constructor_WithEmptyCode_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var ex = Should.Throw<ArgumentException>(() => new ValidatePythonCodeCommand(string.Empty));
        ex.Message.ShouldContain("pythonCode cannot be null, empty, or whitespace");
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexValidCode_ReturnsSuccess()
    {
        // Arrange
        const string complexCode = @"
import talib
import numpy as np

sma_20 = None
sma_50 = None

def initialize(prices):
    global sma_20, sma_50
    sma_20 = talib.SMA(prices['close'], timeperiod=20)
    sma_50 = talib.SMA(prices['close'], timeperiod=50)

def generate_signal(index, price, cash, position):
    if index < 50:
        return {'action': 'hold', 'quantity': 0, 'reason': 'Insufficient data'}

    # Golden cross
    if sma_20[index-1] <= sma_50[index-1] and sma_20[index] > sma_50[index]:
        quantity = int((cash * 0.95) / price)
        return {'action': 'buy', 'quantity': quantity, 'reason': 'Golden cross'}

    # Death cross
    if sma_20[index-1] >= sma_50[index-1] and sma_20[index] < sma_50[index]:
        return {'action': 'sell', 'quantity': position, 'reason': 'Death cross'}

    return {'action': 'hold', 'quantity': 0, 'reason': 'No signal'}
";
        var command = new ValidatePythonCodeCommand(complexCode);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(true, new List<string>())));

        // Act
        Result<AppPythonValidationResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsValid.ShouldBeTrue();
        result.Value.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WhenExecutorThrowsException_ReturnsFailure()
    {
        // Arrange
        const string code = "def generate_signal(index, price, cash, position): pass";
        var command = new ValidatePythonCodeCommand(code);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Throws(new InvalidOperationException("Python runtime not initialized"));

        // Act
        Result<AppPythonValidationResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Python validation failed"));
        result.Errors.ShouldContain(e => e.Message.Contains("Python runtime not initialized"));
        result.Errors.ShouldContain(e => e.Code == "VALIDATION_FAILED");
    }

    [Fact]
    public async Task ExecuteAsync_WithDisallowedImport_ReturnsInvalid()
    {
        // Arrange
        const string codeWithDisallowedImport = @"
import os
import sys

def generate_signal(index, price, cash, position):
    os.system('rm -rf /')  # Malicious code
    return {'action': 'hold', 'quantity': 0, 'reason': 'Evil'}
";
        var command = new ValidatePythonCodeCommand(codeWithDisallowedImport);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(
                false,
                new List<string>
                {
                    "SecurityError: Import 'os' is not allowed",
                    "SecurityError: Import 'sys' is not allowed"
                })));

        // Act
        Result<AppPythonValidationResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsValid.ShouldBeFalse();
        result.Value.Errors.Count.ShouldBe(2);
        result.Value.Errors.ShouldAllBe(e => e.Contains("SecurityError"));
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidReturnValue_ReturnsInvalid()
    {
        // Arrange
        const string codeWithInvalidReturn = @"
def generate_signal(index, price, cash, position):
    return 'invalid'  # Should return dict
";
        var command = new ValidatePythonCodeCommand(codeWithInvalidReturn);

        A.CallTo(() => _fakePythonExecutor.ValidateSyntaxAsync(A<string>._))
            .Returns(Task.FromResult(new DomainPythonValidationResult(
                false,
                new List<string> { "generate_signal must return a dictionary with 'action', 'quantity', 'reason' keys" })));

        // Act
        Result<AppPythonValidationResult> result = await _useCase.ExecuteAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsValid.ShouldBeFalse();
        result.Value.Errors.ShouldContain(e => e.Contains("dictionary"));
    }
}
