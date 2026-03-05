// <copyright file="StrategyStateCard.razor.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using TradingBot.Web.Models;

namespace TradingBot.Web.Components.Features.WeeklyCashStrategy;

/// <summary>
/// Code-behind for StrategyStateCard component.
/// Provides real-time strategy state updates via SignalR.
/// </summary>
public partial class StrategyStateCard
{
    private HubConnection? _hubConnection;

    /// <summary>
    /// Gets or sets the strategy ID to monitor.
    /// </summary>
    [Parameter]
    public Guid StrategyId { get; set; }

    /// <summary>
    /// Gets or sets the strategy name for display.
    /// </summary>
    [Parameter]
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the navigation manager.
    /// </summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets the logger.
    /// </summary>
    [Inject]
    private ILogger<StrategyStateCard> Logger { get; set; } = default!;

    /// <summary>
    /// Gets or sets the current strategy state.
    /// </summary>
    private StrategyStateDto? State { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    private DateTime? LastUpdated { get; set; }

    /// <summary>
    /// Gets a value indicating whether the SignalR connection is established.
    /// </summary>
    private bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await InitializeSignalRConnection();
    }

    private async Task InitializeSignalRConnection()
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(Navigation.ToAbsoluteUri("/hubs/trading"))
                .WithAutomaticReconnect()
                .AddMessagePackProtocol()
                .Build();

            _hubConnection.On<StrategyStateDto>("ReceiveStrategyStateUpdate", async (state) =>
            {
                // Only update if this is our strategy
                if (state.StrategyId == StrategyId)
                {
                    State = state;
                    LastUpdated = DateTime.UtcNow;
                    await InvokeAsync(StateHasChanged);

                    Logger.LogDebug(
                        "Strategy state updated for {StrategyName} ({StrategyId})",
                        state.Name,
                        state.StrategyId);
                }
            });

            _hubConnection.Reconnecting += error =>
            {
                Logger.LogWarning(error, "SignalR reconnecting");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                Logger.LogInformation("SignalR reconnected: {ConnectionId}", connectionId);
                return Task.CompletedTask;
            };

            _hubConnection.Closed += error =>
            {
                if (error != null)
                {
                    Logger.LogError(error, "SignalR connection closed with error");
                }
                else
                {
                    Logger.LogInformation("SignalR connection closed");
                }

                return Task.CompletedTask;
            };

            await _hubConnection.StartAsync();
            Logger.LogInformation("SignalR connection established for strategy {StrategyId}", StrategyId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing SignalR connection");
        }
    }

    private string GetDaysBelowColorClass(int days)
    {
        return days switch
        {
            >= 2 => "text-red-600 dark:text-red-400",
            1 => "text-yellow-600 dark:text-yellow-400",
            _ => "text-green-600 dark:text-green-400",
        };
    }

    private string GetCashRatioColorClass()
    {
        if (State?.CurrentCashRatio == null)
        {
            return "text-gray-900 dark:text-gray-100";
        }

        var ratio = State.CurrentCashRatio.Value;

        if (ratio < State.MinCashRatio)
        {
            return "text-red-600 dark:text-red-400"; // Too low
        }

        if (ratio > State.MaxCashRatio)
        {
            return "text-yellow-600 dark:text-yellow-400"; // Too high
        }

        return "text-green-600 dark:text-green-400"; // Healthy
    }
}
