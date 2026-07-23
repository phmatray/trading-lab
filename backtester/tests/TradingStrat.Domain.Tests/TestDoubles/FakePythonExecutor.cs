using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;

namespace TradingStrat.Domain.Tests.TestDoubles;

/// <summary>
/// Fake implementation of IPythonExecutor for testing PythonScriptStrategy without actual Python.
/// Allows configuration of pre-defined responses for signal generation.
/// </summary>
public class FakePythonExecutor : IPythonExecutor
{
    private bool _initializeCalled;
    private readonly List<PythonSignalResult> _signalResults = new();
    private int _currentSignalIndex;
    private readonly PythonValidationResult _validationResult;
    private readonly Func<int, decimal, decimal, int, PythonSignalResult>? _signalGenerator;

    /// <summary>
    /// Creates fake executor that returns predefined signal results in sequence.
    /// </summary>
    public FakePythonExecutor(params PythonSignalResult[] signalResults)
    {
        _signalResults.AddRange(signalResults);
        _validationResult = new PythonValidationResult(true, new List<string>());
    }

    /// <summary>
    /// Creates fake executor with custom signal generation logic.
    /// </summary>
    public FakePythonExecutor(Func<int, decimal, decimal, int, PythonSignalResult> signalGenerator)
    {
        _signalGenerator = signalGenerator;
        _validationResult = new PythonValidationResult(true, new List<string>());
    }

    /// <summary>
    /// Creates fake executor that returns validation result.
    /// </summary>
    public FakePythonExecutor(PythonValidationResult validationResult)
    {
        _validationResult = validationResult;
    }

    public Task ExecuteInitializeAsync(
        string pythonCode,
        IReadOnlyList<HistoricalPrice> historicalPrices,
        CancellationToken cancellationToken = default)
    {
        _initializeCalled = true;
        return Task.CompletedTask;
    }

    public Task<PythonSignalResult> ExecuteGenerateSignalAsync(
        int currentIndex,
        decimal currentPrice,
        decimal currentCash,
        int currentPosition,
        CancellationToken cancellationToken = default)
    {
        if (_signalGenerator != null)
        {
            return Task.FromResult(_signalGenerator(currentIndex, currentPrice, currentCash, currentPosition));
        }

        if (_signalResults.Count == 0)
        {
            return Task.FromResult(new PythonSignalResult("hold", 0, "No signals configured"));
        }

        if (_currentSignalIndex >= _signalResults.Count)
        {
            // Repeat last signal if we run out
            return Task.FromResult(_signalResults[^1]);
        }

        PythonSignalResult result = _signalResults[_currentSignalIndex];
        _currentSignalIndex++;
        return Task.FromResult(result);
    }

    public Task<PythonValidationResult> ValidateSyntaxAsync(string pythonCode)
    {
        return Task.FromResult(_validationResult);
    }

    public bool InitializeWasCalled => _initializeCalled;
    public int SignalCallCount => _currentSignalIndex;
}
