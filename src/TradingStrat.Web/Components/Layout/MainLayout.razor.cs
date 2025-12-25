using Microsoft.AspNetCore.Components;
using TradingStrat.Web.Services;

namespace TradingStrat.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase, IAsyncDisposable
{
    [Inject] private LocalStorageService LocalStorage { get; set; } = null!;

    // Layout state
    private bool _sidebarCollapsed = false;
    private bool _aiPanelCollapsed = false;

    // TopBar data (placeholder - will be populated from services)
    private readonly string? _selectedPortfolioName = null;
    private readonly decimal? _portfolioValue = null;
    private readonly decimal? _ytdPerformance = null;

    // AI Panel data (placeholder - will be populated from context)
    private readonly string? _currentTicker = null;
    private readonly string? _currentContext = null;
    private readonly string _currentRegime = "NEUTRAL";
    private readonly string? _currentRecommendation = null;
    private readonly int? _confidence = null;
    private readonly List<string>? _reasons = null;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Load collapse states from localStorage (must be in OnAfterRenderAsync for JS interop)
            bool? sidebarState = await LocalStorage.GetItemAsync<bool?>("layout_sidebar_collapsed");
            if (sidebarState.HasValue && sidebarState.Value != _sidebarCollapsed)
            {
                _sidebarCollapsed = sidebarState.Value;
                StateHasChanged();
            }

            bool? aiPanelState = await LocalStorage.GetItemAsync<bool?>("layout_aipanel_collapsed");
            if (aiPanelState.HasValue && aiPanelState.Value != _aiPanelCollapsed)
            {
                _aiPanelCollapsed = aiPanelState.Value;
                StateHasChanged();
            }

            // TODO: Load portfolio data from PortfolioStateService
            // TODO: Load AI recommendations from appropriate service
        }
    }

    private async Task OnSidebarCollapsedChanged(bool value)
    {
        _sidebarCollapsed = value;
        await LocalStorage.SetItemAsync("layout_sidebar_collapsed", _sidebarCollapsed);
    }

    private async Task OnAiPanelCollapsedChanged(bool value)
    {
        _aiPanelCollapsed = value;
        await LocalStorage.SetItemAsync("layout_aipanel_collapsed", _aiPanelCollapsed);
    }

    public async ValueTask DisposeAsync()
    {
        // Cleanup if needed
        await Task.CompletedTask;
    }
}
