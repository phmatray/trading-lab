// <copyright file="ITradingClient.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Core.Models.Backtest;
using TradingBot.Core.Models.Configuration;
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
    Task ReceiveRiskSettingsUpdate(Core.Models.Configuration.RiskSettings settings);

    /// <summary>
    /// Notifies clients when a new position is opened.
    /// </summary>
    /// <param name="position">The newly opened position.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnPositionOpened(Position position);

    /// <summary>
    /// Notifies clients when a position is closed.
    /// </summary>
    /// <param name="positionId">The ID of the closed position.</param>
    /// <param name="trade">The resulting trade record.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnPositionClosed(Guid positionId, Trade trade);

    /// <summary>
    /// Notifies clients of equity updates (total equity, unrealized PnL, realized PnL).
    /// </summary>
    /// <param name="totalEquity">Current total account equity.</param>
    /// <param name="unrealizedPnL">Current unrealized profit/loss.</param>
    /// <param name="realizedPnL">Current realized profit/loss.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnEquityUpdated(decimal totalEquity, decimal unrealizedPnL, decimal realizedPnL);

    /// <summary>
    /// Notifies clients of backtest progress during execution.
    /// </summary>
    /// <param name="backtestId">The unique backtest identifier.</param>
    /// <param name="progressPercent">Progress percentage (0-100).</param>
    /// <param name="statusMessage">Human-readable status message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnBacktestProgress(string backtestId, int progressPercent, string statusMessage);

    /// <summary>
    /// Notifies clients when a backtest completes successfully.
    /// </summary>
    /// <param name="backtestId">The unique backtest identifier.</param>
    /// <param name="result">The complete backtest result.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnBacktestCompleted(string backtestId, BacktestResult result);

    /// <summary>
    /// Notifies clients when a backtest fails with an error.
    /// </summary>
    /// <param name="backtestId">The unique backtest identifier.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnBacktestFailed(string backtestId, string errorMessage);

    /// <summary>
    /// Notifies clients when risk settings are modified.
    /// </summary>
    /// <param name="newSettings">The updated risk settings.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnRiskSettingsChanged(Core.Models.Configuration.RiskSettings newSettings);

    /// <summary>
    /// Notifies clients when a strategy's configuration parameters are changed.
    /// </summary>
    /// <param name="strategyName">The name of the strategy that was configured.</param>
    /// <param name="parameters">The new parameter values.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnStrategyConfigurationChanged(string strategyName, Dictionary<string, object> parameters);
}
