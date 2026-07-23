using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.Services;

/// <summary>
/// Domain service interface for executing Python strategy code.
/// Implemented in Infrastructure layer using Python.NET.
/// Maintains domain layer purity by abstracting Python execution details.
/// </summary>
public interface IPythonExecutor
{
    /// <summary>
    /// Executes Python initialize() function with historical price data.
    /// Called once before backtesting begins to pre-calculate indicators.
    /// </summary>
    /// <param name="pythonCode">Complete Python strategy code</param>
    /// <param name="historicalPrices">Full historical price dataset</param>
    /// <param name="cancellationToken">Timeout enforcement (default 30s)</param>
    /// <exception cref="PythonExecutionException">On syntax errors, runtime errors, or timeout</exception>
    Task ExecuteInitializeAsync(
        string pythonCode,
        IReadOnlyList<HistoricalPrice> historicalPrices,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes Python generate_signal() function for a specific bar.
    /// Called once per bar during backtesting to get trading signals.
    /// </summary>
    /// <param name="currentIndex">Bar index in historical data (0-based)</param>
    /// <param name="currentPrice">Current bar's closing price</param>
    /// <param name="currentCash">Available cash in portfolio</param>
    /// <param name="currentPosition">Current position size (number of shares)</param>
    /// <param name="cancellationToken">Timeout enforcement (default 5s per bar)</param>
    /// <returns>Trading signal from Python code</returns>
    /// <exception cref="PythonExecutionException">On runtime errors or timeout</exception>
    Task<PythonSignalResult> ExecuteGenerateSignalAsync(
        int currentIndex,
        decimal currentPrice,
        decimal currentCash,
        int currentPosition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates Python code syntax without executing it.
    /// Checks for required functions and compilable syntax.
    /// </summary>
    /// <param name="pythonCode">Python code to validate</param>
    /// <returns>Validation result with syntax errors if any</returns>
    Task<PythonValidationResult> ValidateSyntaxAsync(string pythonCode);
}

/// <summary>
/// Result from Python generate_signal() execution.
/// </summary>
/// <param name="Action">Trading action: "buy", "sell", or "hold"</param>
/// <param name="Quantity">Number of shares to trade</param>
/// <param name="Reason">Human-readable explanation for the signal</param>
public record PythonSignalResult(
    string Action,
    int Quantity,
    string Reason
);

/// <summary>
/// Result from Python syntax validation.
/// </summary>
/// <param name="IsValid">True if code is syntactically correct and has required functions</param>
/// <param name="Errors">List of validation error messages</param>
public record PythonValidationResult(
    bool IsValid,
    List<string> Errors
);

/// <summary>
/// Exception thrown when Python execution fails.
/// Wraps Python runtime errors, syntax errors, and timeout exceptions.
/// </summary>
public class PythonExecutionException : Exception
{
    public PythonExecutionException(string message) : base(message) { }
    public PythonExecutionException(string message, Exception inner) : base(message, inner) { }
}
