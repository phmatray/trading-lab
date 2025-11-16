// <copyright file="WeeklyRoutineWorker.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;

namespace TradingBot.Web.BackgroundWorkers;

/// <summary>
/// Background service for executing weekly cash-managed strategy routines on schedule.
/// Runs daily checks and executes weekly routine on configured day of week.
/// </summary>
public sealed class WeeklyRoutineWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITradingCalendar _tradingCalendar;
    private readonly ILogger<WeeklyRoutineWorker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeeklyRoutineWorker"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for scoped services.</param>
    /// <param name="tradingCalendar">Trading calendar service.</param>
    /// <param name="logger">Logger.</param>
    public WeeklyRoutineWorker(
        IServiceProvider serviceProvider,
        ITradingCalendar tradingCalendar,
        ILogger<WeeklyRoutineWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _tradingCalendar = tradingCalendar;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WeeklyRoutineWorker started");

        // Wait for application to fully start (give 5 seconds for initialization)
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        // T057: Use PeriodicTimer for 24-hour interval checks
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        try
        {
            // Run immediately on startup (if conditions met)
            await ExecuteRoutinesAsync(stoppingToken);

            // Then run every 24 hours
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ExecuteRoutinesAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("WeeklyRoutineWorker is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WeeklyRoutineWorker encountered an error");
            throw; // Critical failure - let the host handle restart
        }
    }

    /// <summary>
    /// Checks if the weekly routine should execute today based on configured day of week.
    /// T058: NCrontab-style schedule parsing (simplified to day-of-week matching).
    /// </summary>
    /// <param name="configuredDayOfWeek">Configured day of week (0=Sunday, 6=Saturday).</param>
    /// <param name="currentDate">Current date to check.</param>
    /// <returns>True if routine should execute today.</returns>
    private static bool ShouldExecuteToday(int configuredDayOfWeek, DateTime currentDate)
    {
        // Convert 0-6 (Sunday-Saturday) to DayOfWeek enum
        var targetDayOfWeek = (DayOfWeek)configuredDayOfWeek;

        return currentDate.DayOfWeek == targetDayOfWeek;
    }

    /// <summary>
    /// Executes daily and weekly routines for all enabled strategies if conditions are met.
    /// T063: Daily routine runs every trading day, weekly routine runs on configured day.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ExecuteRoutinesAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        _logger.LogDebug(
            "Checking if routines should execute: Date={Date:yyyy-MM-dd}, DayOfWeek={DayOfWeek}, Time={Time:HH:mm:ss}",
            now.Date,
            now.DayOfWeek,
            now.TimeOfDay);

        // Check if today is a trading day
        if (!_tradingCalendar.IsTradingDay(now))
        {
            _logger.LogDebug(
                "Skipping routine execution - not a trading day: {Date:yyyy-MM-dd} ({DayOfWeek})",
                now.Date,
                now.DayOfWeek);
            return;
        }

        // Create a new scope for scoped services
        using var scope = _serviceProvider.CreateScope();
        var strategyRepository = scope.ServiceProvider.GetRequiredService<IWeeklyCashManagedStrategyRepository>();
        var routineExecutor = scope.ServiceProvider.GetRequiredService<IWeeklyRoutineExecutor>();

        try
        {
            // Get all enabled strategies
            var enabledStrategies = await strategyRepository.GetEnabledStrategiesAsync(cancellationToken);

            if (!enabledStrategies.Any())
            {
                _logger.LogDebug("No enabled strategies found - skipping routine execution");
                return;
            }

            _logger.LogInformation(
                "Found {Count} enabled strategies to process",
                enabledStrategies.Count);

            // T063: Execute DAILY routine for ALL enabled strategies every trading day
            _logger.LogInformation("Starting daily routine for {Count} strategies", enabledStrategies.Count);

            foreach (var strategy in enabledStrategies)
            {
                try
                {
                    await routineExecutor.ExecuteDailyRoutineAsync(strategy, cancellationToken);

                    _logger.LogDebug(
                        "Daily routine completed for {StrategyName}: MA20={MA20:C}, Price={Price:C}, DaysBelowMA20={DaysBelowMA20}",
                        strategy.Name,
                        strategy.CurrentMA20,
                        strategy.CurrentUnderlyingPrice,
                        strategy.DaysBelowMA20);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error executing daily routine for strategy {StrategyId} - {StrategyName}",
                        strategy.Id,
                        strategy.Name);

                    // Continue with other strategies even if one fails
                }
            }

            // Execute WEEKLY routine for each strategy on its configured day
            _logger.LogInformation("Checking for weekly routine execution");

            foreach (var strategy in enabledStrategies)
            {
                // T058: Check if today matches the configured execution day
                if (!ShouldExecuteToday(strategy.ExecutionDayOfWeek, now))
                {
                    _logger.LogDebug(
                        "Skipping weekly routine for {StrategyName} - today is {Today}, configured day is {ConfiguredDay}",
                        strategy.Name,
                        now.DayOfWeek,
                        (DayOfWeek)strategy.ExecutionDayOfWeek);
                    continue;
                }

                _logger.LogInformation(
                    "Executing weekly routine for strategy {StrategyId} - {StrategyName}",
                    strategy.Id,
                    strategy.Name);

                try
                {
                    var result = await routineExecutor.ExecuteWeeklyRoutineAsync(strategy, cancellationToken);

                    _logger.LogInformation(
                        "Weekly routine completed for {StrategyName}: BuyOrder={BuyOrderId}, CashRatio={CashRatio:P2}, Notes={Notes}",
                        strategy.Name,
                        result.BuyOrderId?.ToString() ?? "None",
                        result.CashRatioAfter,
                        result.Notes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error executing weekly routine for strategy {StrategyId} - {StrategyName}",
                        strategy.Id,
                        strategy.Name);

                    // Continue with other strategies even if one fails
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExecuteRoutinesAsync");
            throw;
        }
    }
}
