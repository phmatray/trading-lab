// <copyright file="RealtimeUpdateService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingBot.Core.Interfaces;
using TradingBot.Web.Hubs;

namespace TradingBot.Web.Services;

/// <summary>
/// Background service that broadcasts real-time trading updates to connected clients.
/// </summary>
public sealed class RealtimeUpdateService : IHostedService, IDisposable
{
    private readonly IHubContext<TradingHub, ITradingClient> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RealtimeUpdateService> _logger;
    private Timer? _timer;
    private bool _disposed;

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
        _logger.LogInformation("RealtimeUpdateService is starting");

        // Broadcast updates every 100ms
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

    private async void BroadcastUpdates(object? state)
    {
        try
        {
            // Create a scope for scoped dependencies
            using var scope = _serviceProvider.CreateScope();
            var portfolioManager = scope.ServiceProvider.GetRequiredService<IPortfolioManager>();

            // Get current account state
            var account = await portfolioManager.GetAccountAsync();

            // Broadcast account update to all connected clients
            await _hubContext.Clients.All.ReceiveAccountUpdate(account);

            // Get open positions
            var positions = await portfolioManager.GetPositionsAsync();

            // Broadcast position updates
            foreach (var position in positions)
            {
                await _hubContext.Clients.All.ReceivePositionUpdate(position);
            }

            // Note: Trade updates would be broadcast when positions are closed
            // This is typically triggered by events from the trading engine
            // For simplicity, we're polling account/positions here every 100ms
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting real-time updates");
        }
    }
}
