// <copyright file="ConnectionStatusViewModel.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Models;

/// <summary>
/// SignalR connection state for UI display.
/// </summary>
public sealed class ConnectionStatusViewModel
{
    /// <summary>
    /// Gets or sets the connection status.
    /// </summary>
    public string Status { get; set; } = "disconnected";

    /// <summary>
    /// Gets or sets the last successful connection time.
    /// </summary>
    public DateTime? LastConnected { get; set; }

    /// <summary>
    /// Gets or sets the number of reconnection attempts.
    /// </summary>
    public int ReconnectAttempts { get; set; }

    /// <summary>
    /// Gets a value indicating whether the connection is active.
    /// </summary>
    public bool IsConnected => Status == "connected";
}
