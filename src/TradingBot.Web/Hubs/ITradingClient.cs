// <copyright file="ITradingClient.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Portfolio;
using TradingBot.Core.Models.Risk;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Web.Hubs;

/// <summary>
/// Defines the client-side methods that can be called from the TradingHub.
/// </summary>
public interface ITradingClient
{
    /// <summary>
    /// Receives account update notification.
    /// </summary>
    /// <param name="account">Updated account information.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReceiveAccountUpdate(Account account);

    /// <summary>
    /// Receives position update notification.
    /// </summary>
    /// <param name="position">Updated or new position information.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReceivePositionUpdate(Position position);

    /// <summary>
    /// Receives trade update notification (when a position is closed).
    /// </summary>
    /// <param name="trade">Completed trade information.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReceiveTradeUpdate(Trade trade);

    /// <summary>
    /// Receives connection status update.
    /// </summary>
    /// <param name="status">Connection status ("connected", "reconnecting", "disconnected").</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReceiveConnectionStatus(string status);

    /// <summary>
    /// Receives risk settings update notification.
    /// </summary>
    /// <param name="settings">Updated risk settings.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReceiveRiskSettingsUpdate(RiskSettings settings);
}
