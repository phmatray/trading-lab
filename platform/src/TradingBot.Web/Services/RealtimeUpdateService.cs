// <copyright file="RealtimeUpdateService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using TradingBot.Core.Interfaces;
using TradingBot.Web.Hubs;

namespace TradingBot.Web.Services;

/// <summary>
/// Background service that broadcasts real-time trading updates to connected clients.
/// </summary>
public sealed class RealtimeUpdateService : IHostedService, IDisposable
{
    private static readonly TimeSpan MinimumBroadcastInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan EquityUpdateInterval = TimeSpan.FromSeconds(2);

    private readonly IHubContext<TradingHub, ITradingClient> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RealtimeUpdateService> _logger;
    private readonly Dictionary<Guid, string> _lastPositionHashes = new();
    private readonly HashSet<Guid> _knownPositionIds = new();
    private Timer? _timer;
    private bool _disposed;

    // Caching and change detection
    private string? _lastAccountStateHash;
    private DateTime _lastBroadcastTime = DateTime.MinValue;
    private DateTime _lastEquityUpdateTime = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="RealtimeUpdateService"/> class.
    /// </summary>
    /// <param name="hubContext">The SignalR hub context.</param>
    /// <param name="serviceProvider">The service provider for scoped dependencies.</param>
    /// <param name="logger">The logger instance.</param>
    public RealtimeUpdateService(
        IHubContext<TradingHub, ITradingClient> hubContext,
        IServiceProvider serviceProvider,
        ILogger<RealtimeUpdateService> logger)
    {
        _hubContext = hubContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "RealtimeUpdateService is starting with {Interval}ms throttle",
            MinimumBroadcastInterval.TotalMilliseconds);

        // Check for updates frequently but only broadcast when changes detected or throttle period elapsed
        _timer = new Timer(
            BroadcastUpdates,
            null,
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(100));

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RealtimeUpdateService is stopping");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _timer?.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Computes a simple hash of an object for change detection.
    /// </summary>
    private static string ComputeHash(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)));
    }

    private async void BroadcastUpdates(object? state)
    {
        try
        {
            // Throttle: Only broadcast if minimum interval has elapsed
            var now = DateTime.UtcNow;
            if (now - _lastBroadcastTime < MinimumBroadcastInterval)
            {
                return;
            }

            // Create a scope for scoped dependencies
            using var scope = _serviceProvider.CreateScope();
            var portfolioManager = scope.ServiceProvider.GetRequiredService<IPortfolioManager>();

            var broadcastOccurred = false;

            // Get current account state
            var account = await portfolioManager.GetAccountAsync();
            var accountHash = ComputeHash(account);

            // Only broadcast if account state changed
            if (accountHash != _lastAccountStateHash)
            {
                await _hubContext.Clients.All.ReceiveAccountUpdate(account);
                _lastAccountStateHash = accountHash;
                broadcastOccurred = true;
            }

            // Get open positions
            var positions = await portfolioManager.GetPositionsAsync();
            var currentPositionIds = new HashSet<Guid>();

            // Detect new positions and broadcast position updates
            foreach (var position in positions)
            {
                currentPositionIds.Add(position.Id);
                var positionHash = ComputeHash(position);

                // Check if this is a new position (position opened)
                if (!_knownPositionIds.Contains(position.Id))
                {
                    await _hubContext.Clients.All.OnPositionOpened(position);
                    _knownPositionIds.Add(position.Id);
                    _logger.LogInformation(
                        "Position opened: {Symbol} {Side} {Quantity} @ {Price}",
                        position.Symbol,
                        position.Side,
                        position.Quantity,
                        position.EntryPrice);
                    broadcastOccurred = true;
                }

                // Broadcast position updates only if changed
                if (!_lastPositionHashes.TryGetValue(position.Id, out var lastHash) || lastHash != positionHash)
                {
                    await _hubContext.Clients.All.ReceivePositionUpdate(position);
                    _lastPositionHashes[position.Id] = positionHash;
                    broadcastOccurred = true;
                }
            }

            // Detect closed positions
            var closedPositionIds = _knownPositionIds.Except(currentPositionIds).ToList();
            if (closedPositionIds.Count > 0)
            {
                // Get recent trades to find the most recent one for each closed position
                var trades = await portfolioManager.GetTradeHistoryAsync(
                    startDate: DateTime.UtcNow.AddMinutes(-5),
                    endDate: DateTime.UtcNow);

                foreach (var closedId in closedPositionIds)
                {
                    // Find the most recent trade (assumes trade was just created when position closed)
                    var closedTrade = trades
                        .OrderByDescending(t => t.ExitTime)
                        .FirstOrDefault();

                    if (closedTrade != null)
                    {
                        await _hubContext.Clients.All.OnPositionClosed(closedId, closedTrade);
                        _logger.LogInformation(
                            "Position closed: {PositionId} - {Symbol} - Realized P&L: {PnL}",
                            closedId,
                            closedTrade.Symbol,
                            closedTrade.RealizedPnL);
                    }

                    _knownPositionIds.Remove(closedId);
                    _lastPositionHashes.Remove(closedId);
                    broadcastOccurred = true;
                }
            }

            // Broadcast equity updates every 2 seconds
            if (now - _lastEquityUpdateTime >= EquityUpdateInterval)
            {
                var totalEquity = account.Equity;
                var unrealizedPnL = positions.Sum(p => p.UnrealizedPnL);
                var realizedPnL = account.RealizedPnL;

                await _hubContext.Clients.All.OnEquityUpdated(totalEquity, unrealizedPnL, realizedPnL);
                _lastEquityUpdateTime = now;
                broadcastOccurred = true;
            }

            // Update last broadcast time only if we actually broadcast
            if (broadcastOccurred)
            {
                _lastBroadcastTime = now;
            }

            // Note: Trade updates would be broadcast when positions are closed
            // This is typically triggered by events from the trading engine
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting real-time updates");
        }
    }
}
