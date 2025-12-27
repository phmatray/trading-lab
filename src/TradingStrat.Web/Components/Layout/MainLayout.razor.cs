using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase, IAsyncDisposable
{
    [Inject] private LocalStorageService LocalStorage { get; set; } = null!;
    [Inject] private AiInsightsService AiInsights { get; set; } = null!;
    [Inject] private PortfolioStateService PortfolioState { get; set; } = null!;
    [Inject] private IPortfolioPort PortfolioPort { get; set; } = null!;

    // Layout state
    private bool _sidebarCollapsed = false;
    private bool _aiPanelCollapsed = false;

    // TopBar data
    private string? _selectedPortfolioName = null;
    private decimal? _portfolioValue = null;
    private decimal? _ytdPerformance = null;

    // AI Panel data
    private readonly string? _currentTicker = null;
    private readonly string? _currentContext = null;
    private string _currentRegime = "NEUTRAL";
    private string? _currentRecommendation = null;
    private int? _confidence = null;
    private List<string>? _reasons = null;

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

            // Load portfolio data and AI insights
            await LoadPortfolioDataAsync();
            await LoadAiInsightsAsync();

            // Subscribe to portfolio changes to refresh AI insights
            PortfolioState.OnPortfolioChanged += OnPortfolioChanged;
        }
    }

    private async void OnPortfolioChanged(object? sender, EventArgs e)
    {
        await LoadPortfolioDataAsync();
        await LoadAiInsightsAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadPortfolioDataAsync()
    {
        try
        {
            int? portfolioId = await PortfolioState.GetSelectedPortfolioIdAsync();
            if (portfolioId.HasValue)
            {
                Domain.Entities.Portfolio? portfolio = await PortfolioPort.GetPortfolioByIdAsync(portfolioId.Value);
                if (portfolio != null)
                {
                    _selectedPortfolioName = portfolio.Name;
                    // Portfolio value and YTD performance would require additional calculations
                    // For now, just showing the name
                }
            }
            else
            {
                _selectedPortfolioName = null;
                _portfolioValue = null;
                _ytdPerformance = null;
            }
        }
        catch
        {
            _selectedPortfolioName = null;
            _portfolioValue = null;
            _ytdPerformance = null;
        }
    }

    private async Task LoadAiInsightsAsync()
    {
        try
        {
            // Load market regime
            MarketRegime regime = await AiInsights.GetCurrentRegimeAsync();
            _currentRegime = regime.Regime;

            // Load recommendation
            PortfolioRecommendation recommendation = await AiInsights.GetCurrentRecommendationAsync();
            _currentRecommendation = recommendation.Action;
            _confidence = recommendation.Confidence;
            _reasons = recommendation.Reasons;

            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            _currentRegime = "NEUTRAL";
            _currentRecommendation = null;
            _confidence = null;
            _reasons = null;
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
        // Unsubscribe from portfolio changes
        PortfolioState.OnPortfolioChanged -= OnPortfolioChanged;
        await Task.CompletedTask;
    }
}
