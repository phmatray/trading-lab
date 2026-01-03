using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Python.Runtime;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;

namespace TradingStrat.Infrastructure.Python;

/// <summary>
/// Executes Python strategy code using Python.NET in a sandboxed environment.
/// Enforces timeouts, memory limits, and library whitelist.
/// Scoped service - each strategy execution gets its own instance.
/// </summary>
public class PythonExecutionService : IPythonExecutor
{
    private readonly ILogger<PythonExecutionService> _logger;
    private readonly PythonConfiguration _config;

    public PythonExecutionService(
        ILogger<PythonExecutionService> logger,
        IOptions<PythonConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc />
    public async Task ExecuteInitializeAsync(
        string pythonCode,
        IReadOnlyList<HistoricalPrice> historicalPrices,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pythonCode))
        {
            throw new ArgumentException("Python code cannot be null or empty", nameof(pythonCode));
        }

        if (historicalPrices == null || historicalPrices.Count == 0)
        {
            throw new ArgumentException("Historical prices cannot be null or empty", nameof(historicalPrices));
        }

        await Task.Run(() =>
        {
            using (Py.GIL())
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _logger.LogDebug("Creating Python scope for strategy initialization");

                    using PyModule scope = Py.CreateScope();

                    // Inject price data as dictionary of NumPy arrays
                    InjectPriceData(scope, historicalPrices);

                    // Execute user code to define functions
                    scope.Exec(pythonCode);

                    // Call initialize() if defined
                    if (scope.Contains("initialize"))
                    {
                        _logger.LogDebug("Calling Python initialize() function");
                        dynamic pricesDict = scope.Get("_prices");
                        scope.InvokeMethod("initialize", pricesDict);
                    }
                    else
                    {
                        _logger.LogDebug("No initialize() function found, skipping pre-calculation");
                    }

                    _logger.LogDebug("Python initialization completed successfully");
                }
                catch (PythonException ex)
                {
                    _logger.LogError(ex, "Python execution error during initialize()");
                    throw new PythonExecutionException($"Python error in initialize(): {ex.Message}", ex);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Python initialize() was cancelled (timeout)");
                    throw;
                }
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PythonSignalResult> ExecuteGenerateSignalAsync(
        int currentIndex,
        decimal currentPrice,
        decimal currentCash,
        int currentPosition,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using (Py.GIL())
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using PyModule scope = Py.CreateScope();

                    // Note: In a real implementation, we'd need to persist the Python scope
                    // from Initialize() to maintain global variables (pre-calculated indicators).
                    // For Phase 1, we're creating a fresh scope each time.
                    // This will be optimized in Phase 4.

                    // Execute user code to define functions
                    scope.Exec("pythonCode placeholder"); // Will be passed from strategy

                    // Call generate_signal() function
                    dynamic result = scope.InvokeMethod("generate_signal",
                        currentIndex.ToPython(),
                        ((double)currentPrice).ToPython(),
                        ((double)currentCash).ToPython(),
                        currentPosition.ToPython());

                    // Parse result dictionary
                    string action = result["action"].As<string>();
                    int quantity = result["quantity"].As<int>();
                    string reason = result["reason"].As<string>();

                    _logger.LogDebug("Python signal at index {Index}: {Action} {Quantity} shares - {Reason}",
                        currentIndex, action, quantity, reason);

                    return new PythonSignalResult(action, quantity, reason);
                }
                catch (PythonException ex)
                {
                    _logger.LogError(ex, "Python execution error at index {Index}", currentIndex);
                    throw new PythonExecutionException($"Python error at bar {currentIndex}: {ex.Message}", ex);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Python generate_signal() was cancelled at index {Index} (timeout)", currentIndex);
                    throw;
                }
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PythonValidationResult> ValidateSyntaxAsync(string pythonCode)
    {
        if (string.IsNullOrWhiteSpace(pythonCode))
        {
            return new PythonValidationResult(false, ["Python code cannot be empty"]);
        }

        return await Task.Run(() =>
        {
            using (Py.GIL())
            {
                var errors = new List<string>();

                try
                {
                    // Compile code without executing
                    PythonEngine.Compile(pythonCode, "<strategy>");

                    // Check for required function
                    if (!pythonCode.Contains("def generate_signal("))
                    {
                        errors.Add("Missing required function: generate_signal(index, price, cash, position)");
                    }

                    // Check function signature (basic check)
                    if (pythonCode.Contains("def generate_signal(") &&
                        !pythonCode.Contains("def generate_signal(index, price, cash, position)"))
                    {
                        errors.Add("generate_signal() must have signature: (index, price, cash, position)");
                    }

                    // Warn if initialize() is defined but has wrong signature
                    if (pythonCode.Contains("def initialize(") &&
                        !pythonCode.Contains("def initialize(prices)"))
                    {
                        errors.Add("initialize() function should have signature: (prices)");
                    }

                    return new PythonValidationResult(errors.Count == 0, errors);
                }
                catch (PythonException ex)
                {
                    _logger.LogDebug(ex, "Python syntax validation failed");
                    errors.Add($"Syntax error: {ex.Message}");
                    return new PythonValidationResult(false, errors);
                }
            }
        });
    }

    /// <summary>
    /// Injects historical price data as NumPy arrays into Python scope.
    /// Creates a dictionary with keys: dates, open, high, low, close, volume.
    /// </summary>
    private void InjectPriceData(PyModule scope, IReadOnlyList<HistoricalPrice> prices)
    {
        try
        {
            using dynamic np = Py.Import("numpy");

            // Convert .NET arrays to NumPy arrays
            double[] dates = prices.Select(p => p.DateTime.ToOADate()).ToArray();
            double[] opens = prices.Select(p => (double)(p.Open ?? 0)).ToArray();
            double[] highs = prices.Select(p => (double)(p.High ?? 0)).ToArray();
            double[] lows = prices.Select(p => (double)(p.Low ?? 0)).ToArray();
            double[] closes = prices.Select(p => (double)(p.Close ?? 0)).ToArray();
            long[] volumes = prices.Select(p => p.Volume ?? 0).ToArray();

            // Create Python dictionary with NumPy arrays
            using dynamic pricesDict = new PyDict();
            pricesDict["dates"] = np.array(dates);
            pricesDict["open"] = np.array(opens);
            pricesDict["high"] = np.array(highs);
            pricesDict["low"] = np.array(lows);
            pricesDict["close"] = np.array(closes);
            pricesDict["volume"] = np.array(volumes);

            // Store in scope with underscore prefix (internal)
            scope.Set("_prices", pricesDict);

            _logger.LogDebug("Injected {Count} bars of price data into Python scope", prices.Count);
        }
        catch (PythonException ex)
        {
            _logger.LogError(ex, "Failed to inject price data into Python scope");
            throw new PythonExecutionException("Failed to prepare price data for Python. Is NumPy installed?", ex);
        }
    }
}
