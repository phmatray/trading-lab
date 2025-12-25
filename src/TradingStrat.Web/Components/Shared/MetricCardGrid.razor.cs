using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

public partial class MetricCardGrid : ComponentBase
{
    /// <summary>
    /// List of metrics to display
    /// </summary>
    [Parameter]
    public List<MetricCardData> Metrics { get; set; } = new();

    /// <summary>
    /// Number of columns in the grid (responsive: 1 on mobile, 2 on tablet, Columns on desktop)
    /// </summary>
    [Parameter]
    public int Columns { get; set; } = 4;

    private string GetGridClass()
    {
        return Columns switch
        {
            2 => "md:grid-cols-2",
            3 => "md:grid-cols-2 lg:grid-cols-3",
            4 => "md:grid-cols-2 lg:grid-cols-4",
            5 => "md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5",
            6 => "md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6",
            _ => "md:grid-cols-2 lg:grid-cols-4"
        };
    }

    private string GetValueColorClass(MetricCardData metric)
    {
        if (!metric.IsPositive.HasValue)
        {
            return "text-gray-900 dark:text-dark-text-primary";
        }

        return metric.IsPositive.Value
            ? "metric-positive"
            : "metric-negative";
    }

    private RenderFragment GetIconPath(string iconName) => builder =>
    {
        string path = iconName.ToLowerInvariant() switch
        {
            "cash" => "M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z",
            "trending-up" => "M13 7h8m0 0v8m0-8l-8 8-4-4-6 6",
            "trending-down" => "M13 17h8m0 0V9m0 8l-8-8-4 4-6-6",
            "portfolio" => "M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z",
            "chart" => "M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z",
            "percentage" => "M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z",
            _ => "M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" // default info icon
        };

        builder.OpenElement(0, "path");
        builder.AddAttribute(1, "stroke-linecap", "round");
        builder.AddAttribute(2, "stroke-linejoin", "round");
        builder.AddAttribute(3, "stroke-width", "2");
        builder.AddAttribute(4, "d", path);
        builder.CloseElement();
    };

    /// <summary>
    /// Data model for a single metric card
    /// </summary>
    public class MetricCardData
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public bool? IsPositive { get; set; }
        public string? Icon { get; set; }
    }
}
