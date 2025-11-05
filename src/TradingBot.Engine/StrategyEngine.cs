// <copyright file="StrategyEngine.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Engine;

/// <summary>
/// Engine for managing and executing trading strategies.
/// </summary>
public sealed class StrategyEngine : IStrategyEngine
{
    private readonly ILogger<StrategyEngine> _logger;
    private readonly Dictionary<string, IStrategy> _strategies;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyEngine"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public StrategyEngine(ILogger<StrategyEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _strategies = new Dictionary<string, IStrategy>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
#pragma warning disable CS0067 // Event is never used - subscribed to by SignalProcessor
    public event EventHandler<Signal>? SignalGenerated;
#pragma warning restore CS0067

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
}
