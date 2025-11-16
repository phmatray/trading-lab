// <copyright file="StrategyDetailsPanel.razor.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Components;
using TradingBot.Web.Models;

namespace TradingBot.Web.Components.Features.WeeklyCashStrategy;

/// <summary>
/// Code-behind for StrategyDetailsPanel component.
/// Displays detailed strategy metrics and status information.
/// </summary>
public partial class StrategyDetailsPanel
{
    /// <summary>
    /// Gets or sets the strategy state to display.
    /// </summary>
    [Parameter]
    public StrategyStateDto? State { get; set; }

    private static string GetDayOfWeekName(int dayOfWeek)
    {
        return dayOfWeek switch
        {
            0 => "Sunday",
            1 => "Monday",
            2 => "Tuesday",
            3 => "Wednesday",
            4 => "Thursday",
            5 => "Friday",
            6 => "Saturday",
            _ => "Unknown",
        };
    }

    private static string GetTimeAgo(DateTime timestamp)
    {
        var elapsed = DateTime.UtcNow - timestamp;

        if (elapsed.TotalMinutes < 1)
        {
            return "Just now";
        }

        if (elapsed.TotalHours < 1)
        {
            return $"{(int)elapsed.TotalMinutes} minute{((int)elapsed.TotalMinutes == 1 ? string.Empty : "s")} ago";
        }

        if (elapsed.TotalDays < 1)
        {
            return $"{(int)elapsed.TotalHours} hour{((int)elapsed.TotalHours == 1 ? string.Empty : "s")} ago";
        }

        if (elapsed.TotalDays < 7)
        {
            return $"{(int)elapsed.TotalDays} day{((int)elapsed.TotalDays == 1 ? string.Empty : "s")} ago";
        }

        return timestamp.ToString("MMM dd, yyyy");
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
            return "text-red-600 dark:text-red-400";
        }

        if (ratio > State.MaxCashRatio)
        {
            return "text-yellow-600 dark:text-yellow-400";
        }

        return "text-green-600 dark:text-green-400";
    }

    private string GetCashRatioIndicatorClass()
    {
        if (State?.CurrentCashRatio == null)
        {
            return "bg-gray-600 dark:bg-gray-400";
        }

        var ratio = State.CurrentCashRatio.Value;

        if (ratio < State.MinCashRatio)
        {
            return "bg-red-600 dark:bg-red-400";
        }

        if (ratio > State.MaxCashRatio)
        {
            return "bg-yellow-600 dark:bg-yellow-400";
        }

        return "bg-green-600 dark:bg-green-400";
    }
}
