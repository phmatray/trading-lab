// <copyright file="StrategyUpdateBroadcaster.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using TradingBot.Core.Events;
using TradingBot.Core.Interfaces;
using TradingBot.Web.Hubs;
using TradingBot.Web.Models;

namespace TradingBot.Web.Services;

/// <summary>
/// Service responsible for broadcasting strategy state updates via SignalR.
/// Implements batching with 2-second interval and hash-based change detection.
/// </summary>
public sealed class StrategyUpdateBroadcaster :
    INotificationHandler<StrategyExecutedEvent>,
    INotificationHandler<MA20UpdatedEvent>,
    INotificationHandler<StrategyEnabledEvent>,
    INotificationHandler<StrategyDisabledEvent>,
    INotificationHandler<StrategyConfigurationUpdatedEvent>,
    INotificationHandler<CashBufferAdjustedEvent>,
    IDisposable
{
    private readonly IHubContext<TradingHub, ITradingClient> _hubContext;
    private readonly IWeeklyCashManagedStrategyRepository _strategyRepository;
    private readonly IPortfolioManager _portfolioManager;
    private readonly ILogger<StrategyUpdateBroadcaster> _logger;
    private readonly ConcurrentDictionary<Guid, string> _strategyStateHashes = new();
    private readonly ConcurrentDictionary<Guid, DateTime> _lastBroadcastTimes = new();
    private readonly PeriodicTimer _batchTimer;
    private readonly ConcurrentQueue<Guid> _pendingUpdates = new();
    private readonly Task _batchProcessingTask;
    private readonly CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyUpdateBroadcaster"/> class.
    /// </summary>
    /// <param name="hubContext">The SignalR hub context.</param>
    /// <param name="strategyRepository">The strategy repository.</param>
    /// <param name="portfolioManager">The portfolio manager.</param>
    /// <param name="logger">The logger instance.</param>
    public StrategyUpdateBroadcaster(
        IHubContext<TradingHub, ITradingClient> hubContext,
        IWeeklyCashManagedStrategyRepository strategyRepository,
        IPortfolioManager portfolioManager,
        ILogger<StrategyUpdateBroadcaster> logger)
    {
        _hubContext = hubContext;
        _strategyRepository = strategyRepository;
        _portfolioManager = portfolioManager;
        _logger = logger;

        _batchTimer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        _cancellationTokenSource = new CancellationTokenSource();
        _batchProcessingTask = ProcessBatchedUpdatesAsync(_cancellationTokenSource.Token);
    }

    /// <inheritdoc/>
    public Task Handle(StrategyExecutedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Strategy executed event received for strategy {StrategyId}",
            notification.StrategyId);

        QueueUpdate(notification.StrategyId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Handle(MA20UpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "MA20 updated event received for strategy {StrategyId}",
            notification.StrategyId);

        QueueUpdate(notification.StrategyId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Handle(StrategyEnabledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Strategy enabled event received for strategy {StrategyId}",
            notification.StrategyId);

        QueueUpdate(notification.StrategyId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Handle(StrategyDisabledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Strategy disabled event received for strategy {StrategyId}",
            notification.StrategyId);

        QueueUpdate(notification.StrategyId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Handle(StrategyConfigurationUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Strategy configuration updated event received for strategy {StrategyId}",
            notification.StrategyId);

        QueueUpdate(notification.StrategyId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Handle(CashBufferAdjustedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Cash buffer adjusted event received for strategy {StrategyId}",
            notification.StrategyId);

        QueueUpdate(notification.StrategyId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _batchTimer.Dispose();
        _cancellationTokenSource.Dispose();
    }

    private static string ComputeHash(StrategyStateDto state)
    {
        var json = JsonSerializer.Serialize(state);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    private static DateTime? CalculateNextExecution(TradingBot.Core.Models.Strategy.WeeklyCashManagedStrategy strategy)
    {
        if (!strategy.IsEnabled)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var today = now.Date;
        var targetDayOfWeek = (DayOfWeek)strategy.ExecutionDayOfWeek;

        // Calculate days until next execution day
        var daysUntilTarget = ((int)targetDayOfWeek - (int)today.DayOfWeek + 7) % 7;

        // If today is the execution day and we've already executed, next execution is in 7 days
        if (daysUntilTarget == 0 &&
            strategy.LastExecutionTimestamp.HasValue &&
            strategy.LastExecutionTimestamp.Value.Date == today)
        {
            daysUntilTarget = 7;
        }

        var nextDate = today.AddDays(daysUntilTarget);
        return new DateTime(nextDate.Year, nextDate.Month, nextDate.Day, 21, 0, 0, DateTimeKind.Utc); // 9 PM UTC (market close)
    }

    private void QueueUpdate(Guid strategyId)
    {
        _pendingUpdates.Enqueue(strategyId);
    }

    private async Task ProcessBatchedUpdatesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Strategy update broadcaster started");

        try
        {
            while (await _batchTimer.WaitForNextTickAsync(cancellationToken))
            {
                var strategyIds = new HashSet<Guid>();

                // Collect all pending updates (deduplicating by strategy ID)
                while (_pendingUpdates.TryDequeue(out var strategyId))
                {
                    strategyIds.Add(strategyId);
                }

                if (strategyIds.Count == 0)
                {
                    continue;
                }

                _logger.LogDebug(
                    "Processing batched updates for {Count} strategies",
                    strategyIds.Count);

                // Process each unique strategy
                foreach (var strategyId in strategyIds)
                {
                    try
                    {
                        await BroadcastStrategyStateAsync(strategyId, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error broadcasting strategy state for {StrategyId}",
                            strategyId);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Strategy update broadcaster stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in strategy update broadcaster");
        }
    }

    private async Task BroadcastStrategyStateAsync(Guid strategyId, CancellationToken cancellationToken)
    {
        // Fetch current strategy state
        var strategy = await _strategyRepository.GetByIdAsync(strategyId, cancellationToken);
        if (strategy == null)
        {
            _logger.LogWarning(
                "Strategy {StrategyId} not found, skipping broadcast",
                strategyId);
            return;
        }

        // Get current account state for cash ratio calculation
        var account = await _portfolioManager.GetAccountAsync(cancellationToken);
        var cashRatio = account.Equity > 0 ? account.Cash / account.Equity : 0m;

        // Calculate next scheduled execution
        var nextExecution = CalculateNextExecution(strategy);

        // Build DTO
        var stateDto = new StrategyStateDto
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
            CurrentCashRatio = cashRatio,
            MinCashRatio = strategy.MinCashRatio,
            MaxCashRatio = strategy.MaxCashRatio,
            LastExecutionTimestamp = strategy.LastExecutionTimestamp,
            LastDailyUpdateTimestamp = strategy.LastDailyUpdateTimestamp,
            NextScheduledExecution = nextExecution,
            ExecutionDayOfWeek = strategy.ExecutionDayOfWeek,
            IsBullish = strategy.CurrentUnderlyingPrice.HasValue &&
                        strategy.CurrentMA20.HasValue &&
                        strategy.CurrentUnderlyingPrice.Value > strategy.CurrentMA20.Value,
            IsSellConditionMet = strategy.DaysBelowMA20 >= 2,
            IsBuyConditionMet = strategy.CurrentUnderlyingPrice.HasValue &&
                                strategy.CurrentMA20.HasValue &&
                                strategy.CurrentUnderlyingPrice.Value > strategy.CurrentMA20.Value &&
                                cashRatio > strategy.MinCashRatio,
        };

        // Calculate hash of current state
        var currentHash = ComputeHash(stateDto);

        // Check if state has changed since last broadcast
        if (_strategyStateHashes.TryGetValue(strategyId, out var lastHash) && lastHash == currentHash)
        {
            _logger.LogDebug(
                "Strategy {StrategyId} state unchanged, skipping broadcast",
                strategyId);
            return;
        }

        // Update hash and broadcast time
        _strategyStateHashes[strategyId] = currentHash;
        _lastBroadcastTimes[strategyId] = DateTime.UtcNow;

        // Broadcast to all connected clients
        await _hubContext.Clients.All.ReceiveStrategyStateUpdate(stateDto);

        _logger.LogInformation(
            "Broadcasted strategy state update for {StrategyName} ({StrategyId})",
            strategy.Name,
            strategyId);
    }
}
