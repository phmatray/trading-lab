// <copyright file="WeeklyCashStrategyService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Text.Json;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Strategy;
using TradingBot.Core.ValueObjects;
using TradingBot.Web.Models;

namespace TradingBot.Web.Services;

/// <summary>
/// Service for managing weekly cash-managed strategy configuration and operations.
/// Scoped service for web layer.
/// </summary>
public sealed class WeeklyCashStrategyService
{
    private readonly IWeeklyCashManagedStrategyRepository _strategyRepository;
    private readonly ILogger<WeeklyCashStrategyService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeeklyCashStrategyService"/> class.
    /// </summary>
    /// <param name="strategyRepository">Strategy repository.</param>
    /// <param name="logger">Logger instance.</param>
    public WeeklyCashStrategyService(
        IWeeklyCashManagedStrategyRepository strategyRepository,
        ILogger<WeeklyCashStrategyService> logger)
    {
        _strategyRepository = strategyRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new weekly cash-managed strategy.
    /// </summary>
    /// <param name="dto">Configuration DTO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created strategy ID.</returns>
    public async Task<Guid> CreateStrategyAsync(
        StrategyConfigurationDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new strategy: {StrategyName}", dto.Name);

        // Check if strategy name already exists
        var existing = await _strategyRepository.GetByNameAsync(dto.Name, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Strategy with name '{dto.Name}' already exists");
        }

        // Create breakout rule JSON if enabled
        string? breakoutRuleJson = null;
        if (dto.IsBreakoutRuleEnabled)
        {
            var breakoutConfig = new BreakoutRuleConfig(
                isEnabled: true,
                weeklyPriceIncreaseThreshold: dto.BreakoutPriceThreshold ?? 0.10m,
                volumeMultiplier: dto.BreakoutVolumeMultiplier ?? 1.5m,
                buyRatioMultiplier: dto.BreakoutBuyMultiplier ?? 2.0m);

            breakoutConfig.Validate();
            breakoutRuleJson = JsonSerializer.Serialize(breakoutConfig);
        }

        // Create strategy entity
        var strategy = new WeeklyCashManagedStrategy
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            EtpSymbol = dto.EtpSymbol,
            UnderlyingSymbol = dto.UnderlyingSymbol,
            IsEnabled = false, // Start disabled
            MinCashRatio = dto.MinCashRatio,
            MaxCashRatio = dto.MaxCashRatio,
            WeeklyBuyRatio = dto.WeeklyBuyRatio,
            WeeklySellRatio = dto.WeeklySellRatio,
            ExecutionDayOfWeek = dto.ExecutionDayOfWeek,
            BreakoutRuleConfigJson = breakoutRuleJson,
            DaysBelowMA20 = 0,
            CreatedAt = DateTime.UtcNow,
        };

        strategy.Validate();

        await _strategyRepository.AddAsync(strategy, cancellationToken);
        await _strategyRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Strategy created successfully: {StrategyId} - {StrategyName}",
            strategy.Id,
            strategy.Name);

        return strategy.Id;
    }

    /// <summary>
    /// Updates an existing strategy configuration.
    /// </summary>
    /// <param name="strategyId">Strategy ID.</param>
    /// <param name="dto">Updated configuration DTO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task.</returns>
    public async Task UpdateStrategyAsync(
        Guid strategyId,
        StrategyConfigurationDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating strategy: {StrategyId}", strategyId);

        var strategy = await _strategyRepository.GetByIdAsync(strategyId, cancellationToken);
        if (strategy == null)
        {
            throw new InvalidOperationException($"Strategy with ID '{strategyId}' not found");
        }

        // Create breakout rule JSON if enabled
        string? breakoutRuleJson = null;
        if (dto.IsBreakoutRuleEnabled)
        {
            var breakoutConfig = new BreakoutRuleConfig(
                isEnabled: true,
                weeklyPriceIncreaseThreshold: dto.BreakoutPriceThreshold ?? 0.10m,
                volumeMultiplier: dto.BreakoutVolumeMultiplier ?? 1.5m,
                buyRatioMultiplier: dto.BreakoutBuyMultiplier ?? 2.0m);

            breakoutConfig.Validate();
            breakoutRuleJson = JsonSerializer.Serialize(breakoutConfig);
        }

        // Update configuration using domain method
        var config = new StrategyConfiguration(
            minCashRatio: dto.MinCashRatio,
            maxCashRatio: dto.MaxCashRatio,
            weeklyBuyRatio: dto.WeeklyBuyRatio,
            weeklySellRatio: dto.WeeklySellRatio,
            executionDayOfWeek: dto.ExecutionDayOfWeek,
            breakoutRuleConfigJson: breakoutRuleJson);

        strategy.UpdateConfiguration(config);

        // Update name and symbols (not part of configuration value object)
        strategy.Name = dto.Name;
        strategy.EtpSymbol = dto.EtpSymbol;
        strategy.UnderlyingSymbol = dto.UnderlyingSymbol;

        await _strategyRepository.UpdateAsync(strategy, cancellationToken);
        await _strategyRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Strategy updated successfully: {StrategyId}", strategyId);
    }

    /// <summary>
    /// Enables a strategy.
    /// </summary>
    /// <param name="strategyId">Strategy ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task.</returns>
    public async Task EnableStrategyAsync(Guid strategyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enabling strategy: {StrategyId}", strategyId);

        var strategy = await _strategyRepository.GetByIdAsync(strategyId, cancellationToken);
        if (strategy == null)
        {
            throw new InvalidOperationException($"Strategy with ID '{strategyId}' not found");
        }

        strategy.Enable(); // Domain method raises StrategyEnabledEvent

        await _strategyRepository.UpdateAsync(strategy, cancellationToken);
        await _strategyRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Strategy enabled successfully: {StrategyId}", strategyId);
    }

    /// <summary>
    /// Disables a strategy.
    /// </summary>
    /// <param name="strategyId">Strategy ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task.</returns>
    public async Task DisableStrategyAsync(Guid strategyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Disabling strategy: {StrategyId}", strategyId);

        var strategy = await _strategyRepository.GetByIdAsync(strategyId, cancellationToken);
        if (strategy == null)
        {
            throw new InvalidOperationException($"Strategy with ID '{strategyId}' not found");
        }

        strategy.Disable(); // Domain method raises StrategyDisabledEvent

        await _strategyRepository.UpdateAsync(strategy, cancellationToken);
        await _strategyRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Strategy disabled successfully: {StrategyId}", strategyId);
    }

    /// <summary>
    /// Gets a strategy by ID.
    /// </summary>
    /// <param name="strategyId">Strategy ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Strategy entity or null if not found.</returns>
    public async Task<WeeklyCashManagedStrategy?> GetStrategyAsync(
        Guid strategyId,
        CancellationToken cancellationToken = default)
    {
        return await _strategyRepository.GetByIdAsync(strategyId, cancellationToken);
    }

    /// <summary>
    /// Gets all enabled strategies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of enabled strategies.</returns>
    public async Task<IReadOnlyList<WeeklyCashManagedStrategy>> GetEnabledStrategiesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _strategyRepository.GetEnabledStrategiesAsync(cancellationToken);
    }

    /// <summary>
    /// Converts a strategy entity to a configuration DTO.
    /// </summary>
    /// <param name="strategy">Strategy entity.</param>
    /// <returns>Configuration DTO.</returns>
    public StrategyConfigurationDto ToConfigurationDto(WeeklyCashManagedStrategy strategy)
    {
        var dto = new StrategyConfigurationDto
        {
            Name = strategy.Name,
            EtpSymbol = strategy.EtpSymbol,
            UnderlyingSymbol = strategy.UnderlyingSymbol,
            MinCashRatio = strategy.MinCashRatio,
            MaxCashRatio = strategy.MaxCashRatio,
            WeeklyBuyRatio = strategy.WeeklyBuyRatio,
            WeeklySellRatio = strategy.WeeklySellRatio,
            ExecutionDayOfWeek = strategy.ExecutionDayOfWeek,
            IsBreakoutRuleEnabled = !string.IsNullOrEmpty(strategy.BreakoutRuleConfigJson),
        };

        // Deserialize breakout rule if present
        if (!string.IsNullOrEmpty(strategy.BreakoutRuleConfigJson))
        {
            var breakoutConfig = JsonSerializer.Deserialize<BreakoutRuleConfig>(strategy.BreakoutRuleConfigJson);
            if (breakoutConfig != null)
            {
                dto.BreakoutPriceThreshold = breakoutConfig.WeeklyPriceIncreaseThreshold;
                dto.BreakoutVolumeMultiplier = breakoutConfig.VolumeMultiplier;
                dto.BreakoutBuyMultiplier = breakoutConfig.BuyRatioMultiplier;
            }
        }

        return dto;
    }

    /// <summary>
    /// Converts a strategy entity to a state DTO for real-time updates.
    /// </summary>
    /// <param name="strategy">Strategy entity.</param>
    /// <param name="currentCashRatio">Current cash ratio (optional).</param>
    /// <returns>State DTO.</returns>
    public StrategyStateDto ToStateDto(WeeklyCashManagedStrategy strategy, decimal? currentCashRatio = null)
    {
        var isBullish = strategy.CurrentUnderlyingPrice.HasValue
            && strategy.CurrentMA20.HasValue
            && strategy.CurrentUnderlyingPrice.Value >= strategy.CurrentMA20.Value;

        var isSellConditionMet = strategy.DaysBelowMA20 >= 2;

        var isBuyConditionMet = isBullish
            && currentCashRatio.HasValue
            && currentCashRatio.Value > strategy.MinCashRatio;

        // Calculate next execution
        DateTime? nextExecution = null;
        if (strategy.IsEnabled)
        {
            var now = DateTime.UtcNow;
            var targetDayOfWeek = (DayOfWeek)strategy.ExecutionDayOfWeek;
            var daysUntilExecution = ((int)targetDayOfWeek - (int)now.DayOfWeek + 7) % 7;

            // After 21:00 UTC (market close), schedule for next week
            if (daysUntilExecution == 0 && now.Hour >= 21)
            {
                daysUntilExecution = 7;
            }

            // 21:00 UTC (approximately market close time)
            nextExecution = now.Date.AddDays(daysUntilExecution).AddHours(21);
        }

        return new StrategyStateDto
        {
            StrategyId = strategy.Id,
            Name = strategy.Name,
            IsEnabled = strategy.IsEnabled,
            EtpSymbol = strategy.EtpSymbol,
            UnderlyingSymbol = strategy.UnderlyingSymbol,
            CurrentUnderlyingPrice = strategy.CurrentUnderlyingPrice,
            CurrentEtpPrice = strategy.CurrentEtpPrice,
            CurrentMA20 = strategy.CurrentMA20,
            DaysBelowMA20 = strategy.DaysBelowMA20,
            CurrentCashRatio = currentCashRatio,
            MinCashRatio = strategy.MinCashRatio,
            MaxCashRatio = strategy.MaxCashRatio,
            LastExecutionTimestamp = strategy.LastExecutionTimestamp,
            LastDailyUpdateTimestamp = strategy.LastDailyUpdateTimestamp,
            NextScheduledExecution = nextExecution,
            ExecutionDayOfWeek = strategy.ExecutionDayOfWeek,
            IsBullish = isBullish,
            IsSellConditionMet = isSellConditionMet,
            IsBuyConditionMet = isBuyConditionMet,
        };
    }
}
