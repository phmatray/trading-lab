// <copyright file="StrategyConfigurationRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Configuration;

namespace TradingBot.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for strategy configuration persistence.
/// </summary>
public sealed class StrategyConfigurationRepository : IStrategyConfigurationRepository
{
    private readonly TradingBotDbContext _context;
    private readonly ILogger<StrategyConfigurationRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyConfigurationRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public StrategyConfigurationRepository(
        TradingBotDbContext context,
        ILogger<StrategyConfigurationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<StrategyConfiguration?> GetByStrategyNameAsync(
        string strategyName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving configuration for strategy: {StrategyName}", strategyName);

            var configuration = await _context.StrategyConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(sc => sc.StrategyName == strategyName, cancellationToken);

            if (configuration != null)
            {
                _logger.LogDebug(
                    "Found configuration for strategy {StrategyName} (ID: {Id})",
                    strategyName,
                    configuration.Id);
            }
            else
            {
                _logger.LogDebug("No configuration found for strategy: {StrategyName}", strategyName);
            }

            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving configuration for strategy: {StrategyName}",
                strategyName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<StrategyConfiguration> UpsertAsync(
        StrategyConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Upserting configuration for strategy: {StrategyName}",
                configuration.StrategyName);

            var existing = await _context.StrategyConfigurations
                .FirstOrDefaultAsync(
                    sc => sc.StrategyName == configuration.StrategyName,
                    cancellationToken);

            if (existing != null)
            {
                // Update existing configuration
                existing.ParametersJson = configuration.ParametersJson;
                existing.LastModified = DateTime.UtcNow;

                _logger.LogInformation(
                    "Updated existing configuration for strategy {StrategyName} (ID: {Id})",
                    configuration.StrategyName,
                    existing.Id);
            }
            else
            {
                // Create new configuration
                configuration.Id = Guid.NewGuid();
                configuration.CreatedAt = DateTime.UtcNow;
                configuration.LastModified = DateTime.UtcNow;

                await _context.StrategyConfigurations.AddAsync(configuration, cancellationToken);

                _logger.LogInformation(
                    "Created new configuration for strategy {StrategyName} (ID: {Id})",
                    configuration.StrategyName,
                    configuration.Id);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return existing ?? configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error upserting configuration for strategy: {StrategyName}",
                configuration.StrategyName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string strategyName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting configuration for strategy: {StrategyName}", strategyName);

            var configuration = await _context.StrategyConfigurations
                .FirstOrDefaultAsync(sc => sc.StrategyName == strategyName, cancellationToken);

            if (configuration == null)
            {
                _logger.LogWarning("Configuration not found for strategy: {StrategyName}", strategyName);
                return false;
            }

            _context.StrategyConfigurations.Remove(configuration);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Deleted configuration for strategy {StrategyName} (ID: {Id})",
                strategyName,
                configuration.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration for strategy: {StrategyName}", strategyName);
            throw;
        }
    }
}
