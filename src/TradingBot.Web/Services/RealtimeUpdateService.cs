// <copyright file="RealtimeUpdateService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models;
using TradingBot.Web.Hubs;

namespace TradingBot.Web.Services;

/// <summary>
/// Background service that broadcasts real-time trading updates to connected clients.
/// </summary>
public sealed class RealtimeUpdateService : IHostedService, IDisposable
{
    private static readonly TimeSpan MinimumBroadcastInterval = TimeSpan.FromMilliseconds(500);

    private readonly IHubContext<TradingHub, ITradingClient> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RealtimeUpdateService> _logger;
    private readonly Dictionary<Guid, string> _lastPositionHashes = new();
    private Timer? _timer;
    private bool _disposed;

    // Caching and change detection
    private string? _lastAccountStateHash;
    private DateTime _lastBroadcastTime = DateTime.MinValue;

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
        _logger.LogInformation("RealtimeUpdateService is starting with {Interval}ms throttle", MinimumBroadcastInterval.TotalMilliseconds);

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
        return Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)));
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

            // Broadcast position updates only if changed
            foreach (var position in positions)
            {
                currentPositionIds.Add(position.Id);
                var positionHash = ComputeHash(position);

                if (!_lastPositionHashes.TryGetValue(position.Id, out var lastHash) || lastHash != positionHash)
                {
                    await _hubContext.Clients.All.ReceivePositionUpdate(position);
                    _lastPositionHashes[position.Id] = positionHash;
                    broadcastOccurred = true;
                }
            }

            // Clean up hashes for closed positions
            var closedPositionIds = _lastPositionHashes.Keys.Except(currentPositionIds).ToList();
            foreach (var closedId in closedPositionIds)
            {
                _lastPositionHashes.Remove(closedId);
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
