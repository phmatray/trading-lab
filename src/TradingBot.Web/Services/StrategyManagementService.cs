// <copyright file="StrategyManagementService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Interfaces;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for managing trading strategies.
/// </summary>
public sealed class StrategyManagementService : IStrategyManagementService
{
    private readonly IEnumerable<IStrategy> _strategies;
    private readonly ILogger<StrategyManagementService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyManagementService"/> class.
    /// </summary>
    /// <param name="strategies">The collection of registered strategies.</param>
    /// <param name="logger">The logger instance.</param>
    public StrategyManagementService(
        IEnumerable<IStrategy> strategies,
        ILogger<StrategyManagementService> logger)
    {
        _strategies = strategies;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<List<IStrategy>> GetAllStrategiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading all strategies");

            var strategiesList = _strategies.ToList();

            _logger.LogInformation("Loaded {Count} strategies", strategiesList.Count);

            return Task.FromResult(strategiesList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading strategies");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> EnableStrategyAsync(string strategyName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Enabling strategy: {StrategyName}", strategyName);

            var strategy = _strategies.FirstOrDefault(s => s.Name == strategyName);

            if (strategy == null)
            {
                _logger.LogWarning("Strategy not found: {StrategyName}", strategyName);
                return Task.FromResult(false);
            }

            strategy.Enable();

            _logger.LogInformation("Strategy enabled: {StrategyName}", strategyName);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling strategy: {StrategyName}", strategyName);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> DisableStrategyAsync(string strategyName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Disabling strategy: {StrategyName}", strategyName);

            var strategy = _strategies.FirstOrDefault(s => s.Name == strategyName);

            if (strategy == null)
            {
                _logger.LogWarning("Strategy not found: {StrategyName}", strategyName);
                return Task.FromResult(false);
            }

            strategy.Disable();

            _logger.LogInformation("Strategy disabled: {StrategyName}", strategyName);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling strategy: {StrategyName}", strategyName);
            throw;
        }
    }
}
