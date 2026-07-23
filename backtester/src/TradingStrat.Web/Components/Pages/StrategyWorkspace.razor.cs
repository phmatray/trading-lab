using Microsoft.AspNetCore.Components;
using TradingStrat.Domain.Entities;
using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Pages;

public partial class StrategyWorkspace : ComponentBase, IDisposable
{
    [Inject] private WorkspaceStateService WorkspaceState { get; set; } = null!;
    [Inject] private ProgressService ProgressService { get; set; } = null!;

    private int _activeTab;
    private string _progressMessage = string.Empty;

    private readonly List<Shared.BreadcrumbNav.Breadcrumb> _breadcrumbs = new()
    {
        new() { Label = "Dashboard", Href = "/" },
        new() { Label = "Strategy Workspace", Href = "/workspace" }
    };

    protected override void OnInitialized()
    {
        _activeTab = WorkspaceState.State.ActiveTab;
        WorkspaceState.StateChanged += OnWorkspaceStateChanged;
        ProgressService.OnProgressChanged += OnProgressChanged;
    }

    private void SetActiveTab(int tabIndex)
    {
        _activeTab = tabIndex;
        WorkspaceState.SetActiveTab(tabIndex);
    }

    private string GetTabClass(int tabIndex)
    {
        bool isActive = _activeTab == tabIndex;
        bool isDisabled = IsTabDisabled(tabIndex);

        string baseClass = "border-b-2 font-medium text-sm transition-colors";

        if (isDisabled)
        {
            return $"{baseClass} border-transparent text-gray-400 dark:text-gray-600 cursor-not-allowed";
        }

        if (isActive)
        {
            return $"{baseClass} border-trading-blue text-trading-blue dark:border-dark-accent-blue dark:text-dark-accent-blue";
        }

        return $"{baseClass} border-transparent text-gray-600 dark:text-dark-text-secondary hover:text-trading-blue dark:hover:text-dark-accent-blue hover:border-gray-300 dark:hover:border-gray-600";
    }

    private bool IsTabDisabled(int tabIndex)
    {
        return tabIndex switch
        {
            1 => WorkspaceState.State.CurrentStrategy is null, // Test requires strategy
            2 => WorkspaceState.State.CurrentStrategy is null, // Optimize requires strategy
            3 => WorkspaceState.State.TestResult is null,      // Deploy requires test results
            _ => false
        };
    }

    private void HandleStrategyCreated(CustomStrategy strategy)
    {
        WorkspaceState.SetCurrentStrategy(strategy);

        // Auto-advance to Test tab
        SetActiveTab(1);

        StateHasChanged();
    }

    private void HandleTestComplete(BacktestResult result)
    {
        WorkspaceState.SetTestResult(result);
        StateHasChanged();
    }

    private void HandleOptimizationComplete(WorkspaceOptimizationResult result)
    {
        WorkspaceState.SetOptimizationResult(result);
        StateHasChanged();
    }

    private void ClearWorkspace()
    {
        WorkspaceState.Clear();
        _activeTab = 0;
        StateHasChanged();
    }

    private void OnWorkspaceStateChanged(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    private void OnProgressChanged()
    {
        _progressMessage = ProgressService.CurrentMessage;
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        WorkspaceState.StateChanged -= OnWorkspaceStateChanged;
        ProgressService.OnProgressChanged -= OnProgressChanged;
    }
}
