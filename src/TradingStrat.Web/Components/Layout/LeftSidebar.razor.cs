using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace TradingStrat.Web.Components.Layout;

public partial class LeftSidebar : ComponentBase
{
    /// <summary>
    /// Whether the sidebar is collapsed
    /// </summary>
    [Parameter]
    public bool IsCollapsed { get; set; }

    /// <summary>
    /// Callback when collapse state changes
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsCollapsedChanged { get; set; }

    private readonly List<NavItem> _navItems = new()
    {
        // Analysis Group
        new() { Icon = "home", Label = "Home", Href = "/", Match = NavLinkMatch.All,
                Group = "Analysis", Description = "Overview and quick actions" },
        new() { Icon = "database", Label = "Data", Href = "/data",
                Group = "Analysis", Description = "Import and manage historical data" },
        new() { Icon = "status", Label = "Data Status", Href = "/data/status",
                Group = "Analysis", Description = "View data coverage and quality" },
        new() { Icon = "chart", Label = "Backtest", Href = "/backtest",
                Group = "Analysis", Description = "Test strategies on historical data" },
        new() { Icon = "archive", Label = "Backtest Archive", Href = "/backtests",
                Group = "Analysis", Description = "View backtest history and results" },
        new() { Icon = "lightbulb", Label = "Live Analysis", Href = "/analysis",
                Group = "Analysis", Description = "Real-time market analysis" },
        new() { Icon = "calculator", Label = "A/B Test", Href = "/comparison",
                Group = "Analysis", Description = "Compare strategy performance" },

        // Strategies Group
        new() { Icon = "workspace", Label = "Strategy Workspace", Href = "/workspace",
                Group = "Strategies", Description = "Unified workflow for Define → Test → Optimize → Deploy" },
        new() { Icon = "library", Label = "Strategy Library", Href = "/strategies/library",
                Group = "Strategies", Description = "Browse built-in and custom strategies" },
        new() { Icon = "builder", Label = "Strategy Builder", Href = "/strategies/builder",
                Group = "Strategies", Description = "Create custom trading strategies" },
        new() { Icon = "compare", Label = "Compare Strategies", Href = "/strategies/compare",
                Group = "Strategies", Description = "Compare multiple strategies side-by-side" },
        new() { Icon = "optimize", Label = "Strategy Optimization", Href = "/strategies/optimize",
                Group = "Strategies", Description = "Optimize strategy parameters" },

        // Portfolio Group
        new() { Icon = "portfolio", Label = "Portfolios", Href = "/portfolios",
                Group = "Portfolio", Description = "Manage your portfolios" },

        // System Group
        new() { Icon = "settings", Label = "Settings", Href = "/settings",
                Group = "System", Description = "Application configuration" }
    };

    private string GetSidebarClasses()
    {
        string baseClasses = "fixed top-16 left-0 bottom-0 z-30 transition-all duration-300";
        string widthClasses = IsCollapsed ? "w-16" : "w-64";

        return $"{baseClasses} {widthClasses}";
    }

    private string GetNavLinkClass()
    {
        return "block text-gray-700 dark:text-dark-text-secondary hover:text-trading-blue dark:hover:text-dark-accent-blue hover:bg-gray-50 dark:hover:bg-dark-elevated transition-colors";
    }

    private async Task ToggleCollapse()
    {
        IsCollapsed = !IsCollapsed;
        await IsCollapsedChanged.InvokeAsync(IsCollapsed);
    }

    private string GetTooltipText(NavItem item)
    {
        if (IsCollapsed)
        {
            // When collapsed: show "Label - Description"
            return string.IsNullOrEmpty(item.Description)
                ? item.Label
                : $"{item.Label} - {item.Description}";
        }
        // When expanded: show just description (label is visible)
        return item.Description ?? item.Label;
    }

    private RenderFragment GetIconPath(string iconName) => builder =>
    {
        string path = iconName.ToLowerInvariant() switch
        {
            "home" => "M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6",
            "database" => "M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4",
            "status" => "M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z",
            "chart" => "M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z",
            "archive" => "M5 8h14M5 8a2 2 0 110-4h14a2 2 0 110 4M5 8v10a2 2 0 002 2h10a2 2 0 002-2V8m-9 4h4",
            "lightbulb" => "M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z",
            "calculator" => "M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z",
            "workspace" => "M4 5a1 1 0 011-1h4a1 1 0 011 1v7a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM14 5a1 1 0 011-1h4a1 1 0 011 1v7a1 1 0 01-1 1h-4a1 1 0 01-1-1V5zM4 16a1 1 0 011-1h4a1 1 0 011 1v3a1 1 0 01-1 1H5a1 1 0 01-1-1v-3zM14 16a1 1 0 011-1h4a1 1 0 011 1v3a1 1 0 01-1 1h-4a1 1 0 01-1-1v-3z",
            "library" => "M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253",
            "builder" => "M11 4a2 2 0 114 0v1a1 1 0 001 1h3a1 1 0 011 1v3a1 1 0 01-1 1h-1a2 2 0 100 4h1a1 1 0 011 1v3a1 1 0 01-1 1h-3a1 1 0 01-1-1v-1a2 2 0 10-4 0v1a1 1 0 01-1 1H7a1 1 0 01-1-1v-3a1 1 0 00-1-1H4a2 2 0 110-4h1a1 1 0 001-1V7a1 1 0 011-1h3a1 1 0 001-1V4z",
            "compare" => "M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01",
            "optimize" => "M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4",
            "portfolio" => "M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z",
            "settings" => "M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z",
            _ => "M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
        };

        builder.OpenElement(0, "path");
        builder.AddAttribute(1, "stroke-linecap", "round");
        builder.AddAttribute(2, "stroke-linejoin", "round");
        builder.AddAttribute(3, "stroke-width", "2");
        builder.AddAttribute(4, "d", path);
        builder.CloseElement();
    };

    private class NavItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Href { get; set; } = string.Empty;
        public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;
        public string Group { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
