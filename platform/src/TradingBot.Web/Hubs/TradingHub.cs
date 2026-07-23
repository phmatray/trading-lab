// <copyright file="TradingHub.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.SignalR;

namespace TradingBot.Web.Hubs;

/// <summary>
/// SignalR hub for real-time trading data updates.
/// </summary>
public sealed class TradingHub : Hub<ITradingClient>
{
    private readonly ILogger<TradingHub> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TradingHub"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public TradingHub(ILogger<TradingHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a new client connects to the hub.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Client connected: {ConnectionId}", connectionId);

        // Notify client of successful connection
        await Clients.Caller.ReceiveConnectionStatus("connected");

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        if (exception != null)
        {
            _logger.LogWarning(
                exception,
                "Client disconnected with error: {ConnectionId}",
                connectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", connectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client-to-server method for ping/health check.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Ping()
    {
        await Clients.Caller.ReceiveConnectionStatus("connected");
    }

    /// <summary>
    /// Client-to-server method to subscribe to updates for a specific symbol.
    /// </summary>
    /// <param name="symbol">The trading symbol to subscribe to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SubscribeToSymbol(string symbol)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation(
            "Client {ConnectionId} subscribed to symbol: {Symbol}",
            connectionId,
            symbol);

        // Add connection to a group for this symbol (for future targeted updates)
        await Groups.AddToGroupAsync(connectionId, $"symbol:{symbol}");
    }

    /// <summary>
    /// Client-to-server method to unsubscribe from updates for a specific symbol.
    /// </summary>
    /// <param name="symbol">The trading symbol to unsubscribe from.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UnsubscribeFromSymbol(string symbol)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from symbol: {Symbol}",
            connectionId,
            symbol);

        await Groups.RemoveFromGroupAsync(connectionId, $"symbol:{symbol}");
    }
}
