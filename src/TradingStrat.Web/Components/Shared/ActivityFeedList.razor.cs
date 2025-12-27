using Microsoft.AspNetCore.Components;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Web.Components.Shared;

/// <summary>
/// Activity feed component displaying recent application events.
/// </summary>
public partial class ActivityFeedList : ComponentBase
{
    #region Parameters

    /// <summary>
    /// List of activity events to display.
    /// </summary>
    [Parameter]
    public List<ActivityEvent> Activities { get; set; } = new();

    #endregion

    #region Helper Methods

    private string GetEventIconBackground(string eventType)
    {
        return eventType switch
        {
            "Backtest" => "bg-green-500",
            "Strategy" => "bg-blue-500",
            "Portfolio" => "bg-purple-500",
            "Data" => "bg-orange-500",
            _ => "bg-gray-500"
        };
    }

    private string GetRelativeTime(DateTime timestamp)
    {
        TimeSpan timespan = DateTime.Now - timestamp;

        if (timespan.TotalMinutes < 1)
        {
            return "just now";
        }

        if (timespan.TotalMinutes < 60)
        {
            return $"{(int)timespan.TotalMinutes}m ago";
        }

        if (timespan.TotalHours < 24)
        {
            return $"{(int)timespan.TotalHours}h ago";
        }

        if (timespan.TotalDays < 7)
        {
            return $"{(int)timespan.TotalDays}d ago";
        }

        return timestamp.ToString("MMM d");
    }

    #endregion
}
