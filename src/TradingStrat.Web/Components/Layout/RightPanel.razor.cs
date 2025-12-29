using Microsoft.AspNetCore.Components;
using TradingStrat.Web.Models.State;
using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Layout;

public partial class RightPanel : ComponentBase, IDisposable
{
    [Inject] private RightPanelStateService RightPanelState { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;

    // Parameters for Strategy Copilot functionality
    [Parameter] public string? CurrentTicker { get; set; }
    [Parameter] public string? CurrentContext { get; set; }
    [Parameter] public string CurrentRegime { get; set; } = "NEUTRAL";
    [Parameter] public string? CurrentRecommendation { get; set; }
    [Parameter] public int? Confidence { get; set; }
    [Parameter] public List<string>? Reasons { get; set; }

    // Callback for layout integration (margin adjustment)
    [Parameter] public EventCallback<bool> OnCollapsedChanged { get; set; }

    // State
    private RightPanelTab _activeTab = RightPanelTab.StrategyCopilot;
    private bool _isCollapsed = false;
    private int _unreadCount = 0;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Load panel state from localStorage
            RightPanelState state = await RightPanelState.GetStateAsync();
            _activeTab = state.ActiveTab;
            _isCollapsed = state.IsCollapsed;

            // Load unread count for badge
            _unreadCount = await NotificationService.GetUnreadCountAsync();

            // Subscribe to state and notification changes
            RightPanelState.OnStateChanged += HandleStateChanged;
            NotificationService.OnUnreadCountChanged += HandleUnreadCountChanged;

            StateHasChanged();
        }
    }

    private async Task SetActiveTabAsync(RightPanelTab tab)
    {
        if (_activeTab == tab)
        {
            // Clicking active tab toggles collapse
            await ToggleCollapseAsync();
        }
        else
        {
            // Switch to new tab and expand if collapsed
            _activeTab = tab;
            await RightPanelState.SetActiveTabAsync(tab);

            if (_isCollapsed)
            {
                _isCollapsed = false;
                await RightPanelState.SetCollapsedAsync(false);
                await OnCollapsedChanged.InvokeAsync(false);
            }

            StateHasChanged();
        }
    }

    private async Task ToggleCollapseAsync()
    {
        _isCollapsed = !_isCollapsed;
        await RightPanelState.SetCollapsedAsync(_isCollapsed);
        await OnCollapsedChanged.InvokeAsync(_isCollapsed);
        StateHasChanged();
    }

    private void HandleStateChanged()
    {
        InvokeAsync(async () =>
        {
            RightPanelState state = await RightPanelState.GetStateAsync();
            _activeTab = state.ActiveTab;
            _isCollapsed = state.IsCollapsed;
            StateHasChanged();
        });
    }

    private void HandleUnreadCountChanged(int count)
    {
        InvokeAsync(() =>
        {
            _unreadCount = count;
            StateHasChanged();
        });
    }

    private string GetPanelClasses()
    {
        string baseClasses = "fixed top-16 right-0 bottom-0 z-30 transition-all duration-300";
        string widthClasses = _isCollapsed ? "w-12" : "w-96";
        return $"{baseClasses} {widthClasses}";
    }

    private string GetTabButtonClasses(RightPanelTab tab)
    {
        bool isActive = _activeTab == tab;
        string baseClasses = "flex flex-col items-center justify-center p-3 transition-colors";

        if (isActive && !_isCollapsed)
        {
            return $"{baseClasses} bg-trading-blue dark:bg-dark-accent-blue text-white";
        }

        return $"{baseClasses} text-gray-600 dark:text-dark-text-secondary hover:bg-gray-100 dark:hover:bg-dark-card";
    }

    private string GetAriaLabel()
    {
        return _activeTab switch
        {
            RightPanelTab.Notifications => "Notifications panel",
            RightPanelTab.StrategyCopilot => "Strategy Copilot panel",
            _ => "Right panel"
        };
    }

    private string GetActiveTabTitle()
    {
        return _activeTab switch
        {
            RightPanelTab.Notifications => "Notifications",
            RightPanelTab.StrategyCopilot => "Strategy Copilot",
            _ => "Panel"
        };
    }

    private string GetActiveTabAriaId()
    {
        return $"{_activeTab.ToString().ToLower()}-tab";
    }

    private RenderFragment GetActiveTabIcon() => builder =>
    {
        string svgPath = _activeTab switch
        {
            RightPanelTab.Notifications => "M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9",
            RightPanelTab.StrategyCopilot => "M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z",
            _ => "M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
        };

        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "class", "w-5 h-5");
        builder.AddAttribute(2, "fill", "none");
        builder.AddAttribute(3, "stroke", "currentColor");
        builder.AddAttribute(4, "viewBox", "0 0 24 24");

        builder.OpenElement(5, "path");
        builder.AddAttribute(6, "stroke-linecap", "round");
        builder.AddAttribute(7, "stroke-linejoin", "round");
        builder.AddAttribute(8, "stroke-width", "2");
        builder.AddAttribute(9, "d", svgPath);
        builder.CloseElement();

        builder.CloseElement();
    };

    public void Dispose()
    {
        RightPanelState.OnStateChanged -= HandleStateChanged;
        NotificationService.OnUnreadCountChanged -= HandleUnreadCountChanged;
    }
}
