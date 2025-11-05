// <copyright file="StrategyEngine.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.MarketData;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Engine;

/// <summary>
/// Engine for managing and executing trading strategies.
/// </summary>
public sealed class StrategyEngine : IStrategyEngine, IDisposable
{
    private readonly ILogger<StrategyEngine> _logger;
    private readonly IMarketDataService _marketDataService;
    private readonly Dictionary<string, IStrategy> _strategies;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _backgroundTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyEngine"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="marketDataService">Market data service.</param>
    public StrategyEngine(
        ILogger<StrategyEngine> logger,
        IMarketDataService marketDataService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _marketDataService = marketDataService ?? throw new ArgumentNullException(nameof(marketDataService));
        _strategies = new Dictionary<string, IStrategy>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public event EventHandler<Signal>? SignalGenerated;

    /// <inheritdoc/>
    public bool IsRunning => _backgroundTask != null && !_backgroundTask.IsCompleted;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IStrategy>> GetStrategiesAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _strategies.Values.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IStrategy?> GetStrategyAsync(string name, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _strategies.TryGetValue(name, out var strategy) ? strategy : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public void RegisterStrategy(IStrategy strategy)
    {
        if (strategy == null)
        {
            throw new ArgumentNullException(nameof(strategy));
        }

        _lock.Wait();
        try
        {
            if (_strategies.ContainsKey(strategy.Name))
            {
                _logger.LogWarning("Strategy {Name} is already registered, replacing it", strategy.Name);
            }

            _strategies[strategy.Name] = strategy;
            _logger.LogInformation("Registered strategy: {Name} ({Type})", strategy.Name, strategy.Type);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> EnableStrategyAsync(string name, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_strategies.TryGetValue(name, out var strategy))
            {
                _logger.LogWarning("Strategy {Name} not found", name);
                return false;
            }

            strategy.Enable();
            _logger.LogInformation("Enabled strategy: {Name}", name);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DisableStrategyAsync(string name, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_strategies.TryGetValue(name, out var strategy))
            {
                _logger.LogWarning("Strategy {Name} not found", name);
                return false;
            }

            strategy.Disable();
            _logger.LogInformation("Disabled strategy: {Name}", name);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Executes a strategy and emits any generated signals.
    /// </summary>
    /// <param name="strategyName">Name of the strategy to execute.</param>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="data">Historical market data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated signal, if any.</returns>
    public async Task<Signal?> ExecuteStrategyAsync(
        string strategyName,
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(strategyName))
        {
            throw new ArgumentException("Strategy name cannot be null or empty.", nameof(strategyName));
        }

        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));
        }

        if (data == null || !data.Any())
        {
            throw new ArgumentException("Data cannot be null or empty.", nameof(data));
        }

        await _lock.WaitAsync(cancellationToken);
        IStrategy? strategy;
        try
        {
            if (!_strategies.TryGetValue(strategyName, out strategy))
            {
                _logger.LogWarning("Strategy {Name} not found", strategyName);
                return null;
            }

            if (!strategy.IsEnabled)
            {
                _logger.LogDebug("Strategy {Name} is disabled, skipping execution", strategyName);
                return null;
            }
        }
        finally
        {
            _lock.Release();
        }

        try
        {
            _logger.LogDebug(
                "Executing strategy {Strategy} for {Symbol} with {DataPoints} data points",
                strategyName,
                symbol,
                data.Count);

            var signal = await strategy.GenerateSignalAsync(symbol, data, cancellationToken);

            if (signal != null)
            {
                _logger.LogInformation(
                    "Strategy {Strategy} generated {SignalType} signal for {Symbol} (confidence: {Confidence:P0})",
                    strategyName,
                    signal.Type,
                    signal.Symbol,
                    signal.Confidence);

                // Emit the signal through the event
                SignalGenerated?.Invoke(this, signal);
            }
            else
            {
                _logger.LogDebug(
                    "Strategy {Strategy} generated no signal for {Symbol}",
                    strategyName,
                    symbol);
            }

            return signal;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error executing strategy {Strategy} for {Symbol}",
                strategyName,
                symbol);
            return null;
        }
    }

    /// <summary>
    /// Executes all enabled strategies for the given symbol and data.
    /// </summary>
    /// <param name="symbol">Trading symbol.</param>
    /// <param name="data">Historical market data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of generated signals.</returns>
    public async Task<IReadOnlyList<Signal>> ExecuteAllStrategiesAsync(
        string symbol,
        IReadOnlyList<Candle> data,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));
        }

        if (data == null || !data.Any())
        {
            throw new ArgumentException("Data cannot be null or empty.", nameof(data));
        }

        await _lock.WaitAsync(cancellationToken);
        IEnumerable<IStrategy> enabledStrategies;
        try
        {
            enabledStrategies = _strategies.Values.Where(s => s.IsEnabled).ToList();
        }
        finally
        {
            _lock.Release();
        }

        var signals = new List<Signal>();

        foreach (var strategy in enabledStrategies)
        {
            try
            {
                var signal = await strategy.GenerateSignalAsync(symbol, data, cancellationToken);

                if (signal != null)
                {
                    _logger.LogInformation(
                        "Strategy {Strategy} generated {SignalType} signal for {Symbol}",
                        strategy.Name,
                        signal.Type,
                        signal.Symbol);

                    signals.Add(signal);

                    // Emit the signal through the event
                    SignalGenerated?.Invoke(this, signal);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error executing strategy {Strategy} for {Symbol}",
                    strategy.Name,
                    symbol);
            }
        }

        return signals.AsReadOnly();
    }

    /// <inheritdoc/>
    public Task StartAsync(TimeSpan interval, CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logger.LogWarning("Strategy engine is already running");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Starting strategy engine with interval: {Interval}", interval);

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _backgroundTask = Task.Run(async () => await ExecutionLoopAsync(interval, _cancellationTokenSource.Token), _cancellationTokenSource.Token);

        _logger.LogInformation("Strategy engine started successfully");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            _logger.LogWarning("Strategy engine is not running");
            return;
        }

        _logger.LogInformation("Stopping strategy engine...");

        _cancellationTokenSource?.Cancel();

        if (_backgroundTask != null)
        {
            try
            {
                await _backgroundTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                _logger.LogDebug("Strategy engine execution cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during strategy engine shutdown");
            }
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _backgroundTask = null;

        _logger.LogInformation("Strategy engine stopped");
    }

    /// <summary>
    /// Disposes resources used by the strategy engine.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopAsync().GetAwaiter().GetResult();
        _lock.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Background execution loop that runs strategies at the specified interval.
    /// </summary>
    /// <param name="interval">Execution interval.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExecutionLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Execution loop started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _lock.WaitAsync(cancellationToken);
                List<IStrategy> enabledStrategies;
                try
                {
                    enabledStrategies = _strategies.Values.Where(s => s.IsEnabled).ToList();
                }
                finally
                {
                    _lock.Release();
                }

                if (!enabledStrategies.Any())
                {
                    _logger.LogDebug("No enabled strategies to execute");
                }
                else
                {
                    _logger.LogDebug("Executing {Count} enabled strategies", enabledStrategies.Count);

                    foreach (var strategy in enabledStrategies)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        try
                        {
                            await ExecuteStrategyWithDataAsync(strategy, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Error executing strategy {Strategy}",
                                strategy.Name);
                        }
                    }
                }

                // Wait for the specified interval before next execution
                await Task.Delay(interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in execution loop");

                // Continue running despite errors
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        _logger.LogInformation("Execution loop stopped");
    }

    /// <summary>
    /// Executes a strategy with market data fetched for all its symbols.
    /// </summary>
    /// <param name="strategy">Strategy to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExecuteStrategyWithDataAsync(IStrategy strategy, CancellationToken cancellationToken)
    {
        foreach (var symbol in strategy.Symbols)
        {
            try
            {
                // Fetch historical data for the strategy's timeframe
                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddDays(-100); // Get last 100 days of data

                _logger.LogDebug(
                    "Fetching data for {Symbol} ({Timeframe})",
                    symbol,
                    strategy.Timeframe);

                var candles = await _marketDataService.GetHistoricalDataAsync(
                    symbol,
                    startDate,
                    endDate,
                    strategy.Timeframe,
                    cancellationToken);

                if (candles == null || !candles.Any())
                {
                    _logger.LogWarning(
                        "No market data available for {Symbol}",
                        symbol);
                    continue;
                }

                // Execute strategy with the data
                var signal = await strategy.GenerateSignalAsync(symbol, candles, cancellationToken);

                if (signal != null)
                {
                    _logger.LogInformation(
                        "Strategy {Strategy} generated {SignalType} signal for {Symbol} (confidence: {Confidence:P0})",
                        strategy.Name,
                        signal.Type,
                        signal.Symbol,
                        signal.Confidence);

                    // Emit the signal
                    SignalGenerated?.Invoke(this, signal);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error executing strategy {Strategy} for {Symbol}",
                    strategy.Name,
                    symbol);
            }
        }
    }
}
