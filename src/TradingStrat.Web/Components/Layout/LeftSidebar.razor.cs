using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using TradingStrat.Web.Components.Shared;

namespace TradingStrat.Web.Components.Layout;

public partial class LeftSidebar : ComponentBase
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

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
        // Workspace Group - Primary user workflows
        new() { Icon = "home", Label = "Dashboard", Href = "/", Match = NavLinkMatch.All,
                Group = "Workspace", Description = "Overview and quick actions",
                Category = IconCategory.Workspace },
        new() { Icon = "workspace", Label = "Strategy Workspace", Href = "/workspace",
                Group = "Workspace", Description = "Unified workflow: Define → Test → Optimize → Deploy",
                Category = IconCategory.Workspace },

        // Strategy Research Group - Analysis and comparison tools
        new() { Icon = "library", Label = "Strategy Library", Href = "/strategies/library",
                Group = "Strategy Research", Description = "Browse built-in and custom strategies",
                Category = IconCategory.Strategy },
        new() { Icon = "builder", Label = "Strategy Builder", Href = "/strategies/builder",
                Group = "Strategy Research", Description = "Create custom trading strategies",
                Category = IconCategory.Strategy },
        new() { Icon = "compare", Label = "Compare Strategies", Href = "/strategies/compare",
                Group = "Strategy Research", Description = "Compare multiple strategies side-by-side",
                Category = IconCategory.Strategy },
        new() { Icon = "optimize", Label = "Strategy Optimization", Href = "/strategies/optimize",
                Group = "Strategy Research", Description = "Optimize strategy parameters",
                Category = IconCategory.Strategy },
        new() { Icon = "chart", Label = "Backtest", Href = "/backtest",
                Group = "Strategy Research", Description = "Test strategies on historical data",
                Category = IconCategory.Strategy },
        new() { Icon = "archive", Label = "Backtest Archive", Href = "/backtests",
                Group = "Strategy Research", Description = "View backtest history and results",
                Category = IconCategory.Strategy },
        new() { Icon = "lightbulb", Label = "Live Analysis", Href = "/analysis",
                Group = "Strategy Research", Description = "Real-time market analysis",
                Category = IconCategory.Strategy },

        // Data Management Group - Data operations
        new() { Icon = "database", Label = "Fetch Data", Href = "/data",
                Group = "Data Management", Description = "Import historical market data",
                Category = IconCategory.Data },
        new() { Icon = "status", Label = "Data Status", Href = "/data/status",
                Group = "Data Management", Description = "View data coverage and quality",
                Category = IconCategory.Data },

        // Portfolio Group - Portfolio management
        new() { Icon = "portfolio", Label = "Portfolios", Href = "/portfolios",
                Group = "Portfolio", Description = "Manage your portfolios",
                Category = IconCategory.Portfolio },

        // System Group - Configuration
        new() { Icon = "settings", Label = "Settings", Href = "/settings",
                Group = "System", Description = "Application configuration",
                Category = IconCategory.System }
    };

    private string GetSidebarClasses()
    {
        string baseClasses = "fixed top-16 left-0 bottom-0 z-30 transition-all duration-300";
        string widthClasses = IsCollapsed ? "w-16" : "w-64";

        return $"{baseClasses} {widthClasses}";
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

    private bool IsCurrentRoute(string href, NavLinkMatch match)
    {
        string currentUri = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

        if (match == NavLinkMatch.All)
        {
            return currentUri == href.TrimStart('/');
        }

        return currentUri.StartsWith(href.TrimStart('/'));
    }

    private class NavItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Href { get; set; } = string.Empty;
        public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;
        public string Group { get; set; } = string.Empty;
        public string? Description { get; set; }
        public IconCategory Category { get; set; } = IconCategory.Default;
    }
}
