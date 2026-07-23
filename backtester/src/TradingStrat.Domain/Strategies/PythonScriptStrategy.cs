using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Strategies;

/// <summary>
/// Executes user-provided Python code to generate trading signals.
/// Delegates execution to IPythonExecutor service (implemented in Infrastructure).
/// Enforces timeouts to prevent infinite loops and provides clean error handling.
/// </summary>
public class PythonScriptStrategy : BaseStrategy
{
    private readonly string _pythonCode;
    private readonly IPythonExecutor _pythonExecutor;
    private readonly string _name;
    private readonly string _description;

    /// <summary>
    /// Timeout for Python initialize() function execution.
    /// Prevents long-running initialization from blocking backtest startup.
    /// </summary>
    private static readonly TimeSpan InitializeTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Timeout for Python generate_signal() function execution per bar.
    /// Prevents infinite loops or expensive calculations from hanging backtests.
    /// </summary>
    private static readonly TimeSpan GenerateSignalTimeout = TimeSpan.FromSeconds(5);

    public override string Name => _name;
    public override string Description => _description;

    public PythonScriptStrategy(
        IIndicatorCalculator indicatorCalculator,
        IPythonExecutor pythonExecutor,
        string pythonCode,
        string name,
        string description)
        : base(indicatorCalculator)
    {
        ValidationGuard.Require(pythonExecutor).NotNull();
        ValidationGuard.Require(pythonCode).NotNullOrWhiteSpace();

        _pythonExecutor = pythonExecutor;
        _pythonCode = pythonCode;
        _name = name;
        _description = description;
    }

    public override void Initialize(IReadOnlyList<HistoricalPrice> historicalData)
    {
        base.Initialize(historicalData);

        // Execute Python initialize() function with timeout
        using var cts = new CancellationTokenSource(InitializeTimeout);

        try
        {
            _pythonExecutor.ExecuteInitializeAsync(_pythonCode, historicalData, cts.Token)
                .GetAwaiter()
                .GetResult();
        }
        catch (OperationCanceledException)
        {
            throw new PythonExecutionException(
                $"Python initialize() timed out after {InitializeTimeout.TotalSeconds} seconds");
        }
    }

    public override TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition)
    {
        if (currentIndex >= ClosePrices.Length)
        {
            return new TradeSignal(SignalType.Hold, 0, 0, "Insufficient data");
        }

        decimal currentPrice = ClosePrices[currentIndex];

        // Execute Python generate_signal() with timeout
        using var cts = new CancellationTokenSource(GenerateSignalTimeout);

        try
        {
            PythonSignalResult result = _pythonExecutor.ExecuteGenerateSignalAsync(
                    currentIndex, currentPrice, currentCash, currentPosition, cts.Token)
                .GetAwaiter()
                .GetResult();

            return ConvertPythonSignalToTradeSignal(result, currentPrice);
        }
        catch (OperationCanceledException)
        {
            throw new PythonExecutionException(
                $"Python generate_signal() timed out at index {currentIndex} after {GenerateSignalTimeout.TotalSeconds} seconds");
        }
    }

    private TradeSignal ConvertPythonSignalToTradeSignal(PythonSignalResult result, decimal currentPrice)
    {
        return result.Action.ToLowerInvariant() switch
        {
            "buy" => new TradeSignal(SignalType.Buy, currentPrice, result.Quantity, result.Reason),
            "sell" => new TradeSignal(SignalType.Sell, currentPrice, result.Quantity, result.Reason),
            "hold" => new TradeSignal(SignalType.Hold, 0, 0, result.Reason),
            _ => throw new PythonExecutionException(
                $"Invalid action from Python: '{result.Action}'. Expected 'buy', 'sell', or 'hold'.")
        };
    }

    public override Dictionary<string, object> GetParameters()
    {
        return new Dictionary<string, object>
        {
            { "StrategyType", "Python" },
            { "CodeLength", _pythonCode.Length },
            { "InitializeTimeout", $"{InitializeTimeout.TotalSeconds}s" },
            { "SignalTimeout", $"{GenerateSignalTimeout.TotalSeconds}s" }
        };
    }
}
